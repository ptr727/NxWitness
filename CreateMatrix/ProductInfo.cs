using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace CreateMatrix;

public class ProductInfo
{
    // JSON serialized must be public get and set

    // Serialize enums as strings
    // Use same spelling as used in Makefile
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProductType
    {
        None,
        NxMeta,
        NxWitness,
        // ReSharper disable once InconsistentNaming
        DWSpectrum
    }

    public ProductType Product { get; set; }

    public List<VersionInfo> Versions { get; set; } = [];

    private string GetProductShortName()
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return Product switch
        {
            ProductType.NxMeta => "metavms",
            ProductType.NxWitness => "default",
            ProductType.DWSpectrum => "digitalwatchdog",
            _ => throw new InvalidEnumArgumentException(nameof(Product))
        };
    }

    private static IEnumerable<ProductType> GetProductTypes()
    {
        // Create list of product types
        return Enum.GetValues(typeof(ProductType)).Cast<ProductType>().Where(productType => productType != ProductType.None).ToList();
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

    private void GetReleasesVersions()
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
            List<VersionInfo.LabelType> labelList = [];

            // Get all releases
            var releasesList = ReleasesJsonSchema.GetReleases(httpClient, GetProductShortName());
            foreach (var release in releasesList)
            {
                // We expect only "vms" products
                Debug.Assert(release.Product.Equals("vms", StringComparison.OrdinalIgnoreCase));

                // Set version
                VersionInfo versionInfo = new();
                Debug.Assert(!string.IsNullOrEmpty(release.Version));
                versionInfo.SetVersion(release.Version);

                // Get the build number from the version
                var buildNumber = versionInfo.GetBuildNumber();

                // Get packages for this release
                var packageList = PackagesJsonSchema.GetPackages(httpClient, GetProductShortName(), buildNumber);

                // Get the x64 and arm64 server ubuntu packages
                var packageX64 = packageList.Find(item =>
                    item.Component.Equals("server", StringComparison.OrdinalIgnoreCase) &&
                    item.PlatformName.Equals("linux_x64", StringComparison.OrdinalIgnoreCase) &&
                    item.Variants.Any(variant => variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase)));
                Debug.Assert(packageX64 != default(PackagesJsonSchema.Package));
                Debug.Assert(!string.IsNullOrEmpty(packageX64.File));
                var packageArm64 = packageList.Find(item =>
                    item.Component.Equals("server", StringComparison.OrdinalIgnoreCase) &&
                    item.PlatformName.Equals("linux_arm64", StringComparison.OrdinalIgnoreCase) &&
                    item.Variants.Any(variant => variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase)));
                Debug.Assert(packageArm64 != default(PackagesJsonSchema.Package));
                Debug.Assert(!string.IsNullOrEmpty(packageArm64.File));

                // Create the download URLs
                // https://updates.networkoptix.com/{product}/{build}/{file}
                versionInfo.UriX64 = $"https://updates.networkoptix.com/{GetProductShortName()}/{buildNumber}/{packageX64.File}";
                versionInfo.UriArm64 = $"https://updates.networkoptix.com/{GetProductShortName()}/{buildNumber}/{packageArm64.File}";

                // Set a label based on the publications_type value
                switch (release.PublicationType)
                {
                    case "release":
                    {
                        // Set as stable or latest based on released or not
                        AddLabel(labelList, versionInfo, release.IsReleased() ? VersionInfo.LabelType.Stable : VersionInfo.LabelType.Latest);

                        break;
                    }
                    case "rc":
                    {
                        // Set as rc
                        AddLabel(labelList, versionInfo, VersionInfo.LabelType.RC);

                        break;
                    }
                    case "beta":
                    {
                        // Set as beta
                        AddLabel(labelList, versionInfo, VersionInfo.LabelType.Beta);

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
            if (Versions.FindIndex(item => item.Labels.Contains(VersionInfo.LabelType.Latest)) == -1)
            {
                // Find stable or rc or beta
                var latest = Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Stable));
                latest ??= Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.RC));
                latest ??= Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Beta));
                Debug.Assert(latest != default(VersionInfo));

                // Add latest
                latest.Labels.Add(VersionInfo.LabelType.Latest);
            }

            // If no stable label is set, use latest as stable
            if (Versions.FindIndex(item => item.Labels.Contains(VersionInfo.LabelType.Stable)) == -1)
            {
                // Find latest
                var stable = Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Latest));
                Debug.Assert(stable != default(VersionInfo));

                // Add the stable label
                stable.Labels.Add(VersionInfo.LabelType.Stable);
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
        if (labelList.Exists(item => item.Equals(label))) 
            return;
        labelList.Add(label);
        versionInfo.Labels.Add(label);
    }

    public void LogInformation()
    {
        foreach (var versionUri in Versions)
            Log.Logger.Information("{Product}: Version: {Version}, Label: {Labels}, UriX64: {UriX64}, UriArm64: {UriArm64}", Product, versionUri.Version, versionUri.Labels, versionUri.UriX64, versionUri.UriArm64);
    }

    public void VerifyUrls()
    {
        try
        {
            using HttpClient httpClient = new();
            foreach (var versionUri in Versions)
            { 
                // Will throw on error
                VerifyUrl(httpClient, versionUri.UriX64);
                VerifyUrl(httpClient, versionUri.UriArm64);
            }
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