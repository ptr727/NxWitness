using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace CreateMatrix;

public class ProductInfo
{
    // Serialize enums as strings
    // Use same spelling as used in Makefile
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProductType
    {
        None,
        NxMeta,
        NxWitness,
        DWSpectrum
    }

    public ProductType Product { get; set; }

    public List<VersionInfo> Versions { get; set; } = new();

    private string ProductShortName => GetProductShortName(Product);
    private string ProductCloudHost => GetProductCloudHost(Product);

    private static string GetProductShortName(ProductType productType)
    {
        return productType switch
        {
            ProductType.NxMeta => "metavms",
            ProductType.NxWitness => "default",
            ProductType.DWSpectrum => "digitalwatchdog",
            _ => throw new InvalidEnumArgumentException(nameof(productType))
        };
    }

    private static string GetProductCloudHost(ProductType productType)
    {
        return productType switch
        {
            ProductType.NxMeta => "https://meta.nxvms.com",
            ProductType.NxWitness => "https://nxvms.com",
            ProductType.DWSpectrum => "https://dwspectrum.digital-watchdog.com",
            _ => throw new InvalidEnumArgumentException(nameof(productType))
        };
    }

    public static List<ProductType> GetProductTypes()
    {
        // Create list of product types
        var productList = new List<ProductType>();
        foreach (ProductType productType in Enum.GetValues(typeof(ProductType)))
        {
            // Exclude None type
            if (productType != ProductType.None)
            {
                productList.Add(productType);
            }
        }
        return productList;
    }

    public static List<ProductInfo> GetProducts()
    {
        // Create list of all known products
        return (from ProductType productType in GetProductTypes() select new ProductInfo { Product = productType }).ToList();
    }

    public void GetVersions()
    {
        GetReleasesVersions();
    }

    public void GetDownloadsVersions()
    {
        // Get version information using {cloudhost}/api/utils/downloads
        // Stable and Latest will use the same versions
        Log.Logger.Information("{Product}: Getting online downloads information...", Product);
        try
        {
            // Reuse HttpClient
            using HttpClient httpClient = new();

            // Get downloads
            var downloadsSchema = DownloadsJsonSchema.GetDownloads(httpClient, ProductCloudHost);

            // There is only one version in the downloads API
            VersionInfo versionUri = new();

            // "version": "5.0.0.35134 R10",
            // "version": "4.2.0.32842",
            Debug.Assert(!string.IsNullOrEmpty(downloadsSchema.Version));
            versionUri.SetCleanVersion(downloadsSchema.Version);

            // "buildNumber": "35134",
            Debug.Assert(!string.IsNullOrEmpty(downloadsSchema.BuildNumber));
            var buildNumber = int.Parse(downloadsSchema.BuildNumber);
            Debug.Assert(buildNumber == versionUri.BuildNumber);

            // "installers": [
            // "platform": "linux_x64" (v5)
            // "platform": "linux64", (v4)
            // "appType": "server"
            // "path": "linux/metavms-server-5.0.0.35134-linux_x64.deb",
            Debug.Assert(downloadsSchema.Installers.Count > 0);
            var installer = downloadsSchema.Installers.First(item =>
                (item.PlatformName.Equals("linux_x64", StringComparison.OrdinalIgnoreCase) ||
                 item.PlatformName.Equals("linux64", StringComparison.OrdinalIgnoreCase)) &&
                item.AppType.Equals("server", StringComparison.OrdinalIgnoreCase));
            ArgumentNullException.ThrowIfNull(installer);
            Debug.Assert(!string.IsNullOrEmpty(installer.Path));
            Debug.Assert(!string.IsNullOrEmpty(installer.FileName));

            // Create the download URL
            // https://updates.networkoptix.com/{product}/{build}/{file}
            versionUri.Uri = $"https://updates.networkoptix.com/{ProductShortName}/{buildNumber}/{installer.Path}";

            // Set as "stable" and "latest" labels
            versionUri.Labels.Add(VersionInfo.LabelType.stable);
            versionUri.Labels.Add(VersionInfo.LabelType.latest);

            // Add to list
            Versions.Add(versionUri);
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            // Log and rethrow
            throw;
        }
    }

    public void GetReleasesVersions()
    {
        // Get version information using releases.json and package.json 
        Log.Logger.Information("{Product}: Getting online release information...", Product);
        try
        {
            // Use release version discovery per Network Optix
            // https://support.networkoptix.com/hc/en-us/community/posts/7615204163607-Automating-downloaded-product-versions-using-JSON-API
            // Logic follows similar patterns as used in C++ Desktop Client
            // https://github.com/networkoptix/nx_open/blob/526967920636d3119c92a5220290ecc10957bf12/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp#L57
            // releases_info.cpp : selectVmsRelease(), canReceiveUnpublishedBuild()

            // Reuse HttpClient
            using HttpClient httpClient = new();

            // Labels used for state tracking
            List<VersionInfo.LabelType> labelList = new();

            // Get all releases
            var releasesList = ReleasesJsonSchema.GetReleases(httpClient, ProductShortName);
            foreach (var release in releasesList)
            {
                // We expect only "vms" products
                Debug.Assert(release.Product.Equals("vms"));

                // Set version
                VersionInfo versionInfo = new();
                Debug.Assert(!string.IsNullOrEmpty(release.Version));
                versionInfo.SetCleanVersion(release.Version);

                // Get the build number from the version
                var buildNumber = versionInfo.BuildNumber;

                // Get package for this release
                var package = PackagesJsonSchema.GetPackage(httpClient, ProductShortName, buildNumber);
                Debug.Assert(!string.IsNullOrEmpty(package.File));

                // Create the download URL
                // https://updates.networkoptix.com/{product}/{build}/{file}
                versionInfo.Uri = $"https://updates.networkoptix.com/{ProductShortName}/{buildNumber}/{package.File}";

                // Set a label based on the publications_type value
                switch (release.PublicationType)
                {
                    case "release":
                    {
                        // Set as stable or latest based on released or not
                        AddLabel(labelList, versionInfo, release.IsReleased() ? VersionInfo.LabelType.stable : VersionInfo.LabelType.latest);

                        break;
                    }
                    case "rc":
                    {
                        // Set as rc
                        AddLabel(labelList, versionInfo, VersionInfo.LabelType.rc);

                        break;
                    }
                    case "beta":
                    {
                        // Set as beta
                        AddLabel(labelList, versionInfo, VersionInfo.LabelType.beta);

                        break;
                    }
                    default:
                        // Unknown publication type
                        throw new InvalidEnumArgumentException($"Unknown PublicationType: {release.PublicationType}");
                }

                // Add to list
                Versions.Add(versionInfo);
            }

            // If no latest label is set, use stable or rc or beta as latest
            if (Versions.FindIndex(item => item.Labels.Contains(VersionInfo.LabelType.latest)) == -1)
            {
                // Find stable or rc or beta
                var latest = Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.stable));
                latest ??= Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.rc));
                latest ??= Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.beta));
                Debug.Assert(latest != default(VersionInfo));

                // Add latest
                latest.Labels.Add(VersionInfo.LabelType.latest);
            }

            // If no stable label is set, use latest as stable
            if (Versions.FindIndex(item => item.Labels.Contains(VersionInfo.LabelType.stable)) == -1)
            {
                // Find latest
                var stable = Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.latest));
                Debug.Assert(stable != default(VersionInfo));

                // Add the stable label
                stable.Labels.Add(VersionInfo.LabelType.stable);
            }

            // Sort the labels to make diffs easier
            Versions.ForEach(item => item.Labels.Sort());
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            // Log and rethrow
            throw;
        }
    }

    private static void AddLabel(List<VersionInfo.LabelType> labelList, VersionInfo versionInfo, VersionInfo.LabelType label)
    {
        // Add label only if not already set
        if (!labelList.Exists(item => item.Equals(label)))
        {
            labelList.Add(label);
            versionInfo.Labels.Add(label);
        }
    }

    public void LogInformation()
    {
        foreach (var versionUri in Versions)
        {
            Log.Logger.Information("{Product}: Version: {Version}, Label: {Labels}, Uri: {Uri}", Product,
                versionUri.Version, versionUri.Labels, versionUri.Uri);
        }
    }

    public void VerifyUrls()
    {
        try
        {
            using HttpClient httpClient = new();
            foreach (var versionUri in Versions)
                VerifyUrl(httpClient, versionUri.Uri);
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            // Log and rethrow
            throw;
        }
    }

    private static void VerifyUrl(HttpClient httpClient, string url)
    {
        // Will throw on failure

        // Get URL
        var uri = new Uri(url);
        Log.Logger.Information("Verifying Url: {Url}", url);
        var httpResponse = httpClient.GetAsync(uri).Result;
        httpResponse.EnsureSuccessStatusCode();

        // Get filename from httpResponse or Uri path
        string? fileName = null;
        fileName ??= httpResponse.Content.Headers.ContentDisposition?.FileName;
        fileName ??= Path.GetFileName(uri.LocalPath);

        // Log details
        Log.Logger.Information("File Name: {FileName}, File Size: {FileSize}, Last Modified: {LastModified}",
            fileName,
            httpResponse.Content.Headers.ContentLength,
            httpResponse.Content.Headers.LastModified);
    }
}