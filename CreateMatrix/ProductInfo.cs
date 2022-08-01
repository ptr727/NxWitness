using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using static CreateMatrix.ReleasesJsonSchema;

namespace CreateMatrix;

public class ProductInfo
{
    // Serialize enums as strings
    // Use same spelling as used in Makefile
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProductType
    {
        NxMeta,
        NxWitness,

        // ReSharper disable once InconsistentNaming
        DWSpectrum
    }

    public ProductType Product { get; set; }

    public List<VersionUri> Versions { get; set; } = new();

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

    public static List<ProductInfo> GetProducts()
    {
        // Create list of all known products
        return (from ProductType productType in Enum.GetValues(typeof(ProductType))
            select new ProductInfo { Product = productType }).ToList();
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
            VersionUri versionUri = new();

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
            versionUri.Labels.Add(VersionUri.StableLabel);
            versionUri.Labels.Add(VersionUri.LatestLabel);

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
            // TODO: Questions for Nx
            // - Do we need to use package_urls, or are the downloads always updates.networkoptix.com/{product}?
            // - If we use the build number for the download URL, can we be sure that build numbers are not reused between versions?
            // - What is the significance of the order of items in "packages_urls"?
            // - What is the significance of the order of items in and "releases"?
            //   - Is the first listed release the latest and the second the stable?
            //   - Can we relay on the order or should we sort by semver?
            // - How should I interpret the "release" and "beta" publication types?
            //   - Is the "publication_type" "beta" / "release" reliable to get pre-release builds?
            //   - Can I use the first "release" as latest, and the second as stable?
            //   - If there is one "beta" then there is only one "release", expect "beta", "release", "release"?
            // - The package downloads are zip files not installer files.
            //   - Releases do have DEB files published with the same name, but "beta"" releases only have ZIP files, can DEB files always be published?
            //   - The ZIP filename and the DEB file inside the ZIP is not the same for "beta" releases, this makes automating this difficult.
            //     E.g. "metavms-server_update-5.1.0.35151-linux_x64-beta.zip" contains "metavms-server-5.1.0.35151-linux_x64-beta.deb".
            // - From analysis below, there is a discrepancy between versions reported by the cloud portal and the releases API.
            //   - How to reconcile?

            // NxWitness:
            // Downloads API: 5.0.0.35136
            // Releases API: 5.0.0.35270, 4.2.0.32840
            // Cloud Portal: 5.0.0.35136
            // Beta Portal: Stable: 5.0.0.35270
            // Logic: Latest: 5.0.0.35270, Stable: 4.2.0.32840
            // Discrepancy: Cloud portal and releases versions do not match?

            // NxMeta:
            // Downloads API: 5.0.0.35134 R10
            // Releases API: 5.1.0.35151 R1 (beta), 5.0.0.35134 R10
            // Cloud Portal: 5.0.0.35134 R10
            // Beta Portal: Release: 5.0.0.35134 R10, Beta: 5.1.0.35151 R1
            // Logic: Beta: 5.1.0.35151, Latest: 5.0.0.35134, Stable: 5.0.0.35134

            // DWSpectrum:
            // Downloads API: 4.2.0.32842
            // Releases API: 5.0.0.35271, 4.2.0.32842
            // Cloud Portal: 4.2.0.32842
            // Logic: Latest: 5.0.0.35271, Stable: 4.2.0.32842

            // Reuse HttpClient
            using HttpClient httpClient = new();

            // Keep track of labels
            List<string> labelsList = new();

            // Get all releases
            var releasesList = ReleasesJsonSchema.GetReleases(httpClient, ProductShortName);
            foreach (var release in releasesList)
            {
                // Set version
                VersionUri versionUri = new();
                Debug.Assert(!string.IsNullOrEmpty(release.Version));
                versionUri.SetCleanVersion(release.Version);

                // Get the build number from the version
                var buildNumber = versionUri.BuildNumber;

                // Get package for this release
                var package = PackagesJsonSchema.GetPackage(httpClient, ProductShortName, buildNumber);
                Debug.Assert(!string.IsNullOrEmpty(package.File));

                // Create the download URL
                // https://updates.networkoptix.com/{product}/{build}/{file}
                versionUri.Uri = $"https://updates.networkoptix.com/{ProductShortName}/{buildNumber}/{package.File}";

                // Set a label based on the publications_type value
                switch (release.PublicationType)
                {
                    case "release":
                    {
                        // Set as "latest" or "stable" or no label
                        if (!labelsList.Exists(item => item.Equals(VersionUri.LatestLabel)))
                        {
                            // Latest not yet set, add
                            labelsList.Add(VersionUri.LatestLabel);
                            versionUri.Labels.Add(VersionUri.LatestLabel);
                        }
                        else if (!labelsList.Exists(item => item.Equals(VersionUri.StableLabel)))
                        {
                            // Stable not yet set, add
                            labelsList.Add(VersionUri.StableLabel);
                            versionUri.Labels.Add(VersionUri.StableLabel);
                        }

                        break;
                    }
                    case "beta":
                    {
                        // Set as beta
                        if (!labelsList.Exists(item => item.Equals(VersionUri.BetaLabel)))
                        {
                            // Beta not yet set, add
                            labelsList.Add(VersionUri.BetaLabel);
                            versionUri.Labels.Add(VersionUri.BetaLabel);
                        }

                        break;
                    }
                    default:
                        // Figure out how to handle other release types as we see them
                        throw new InvalidEnumArgumentException($"Unknown PublicationType: {release.PublicationType}");
                }

                // Add to list
                Versions.Add(versionUri);
            }

            // Is there a stable label
            if (Versions.FindIndex(item => item.Labels.Contains(VersionUri.StableLabel)) == -1)
            {
                // Find the version containing the latest label
                var latest = Versions.Find(item => item.Labels.Contains(VersionUri.LatestLabel));
                Debug.Assert(latest != default(VersionUri));

                // Add the stable label
                latest.Labels.Add(VersionUri.StableLabel);
            }

            // Check logic to make sure latest and stable are present
            Debug.Assert(Versions.FindIndex(item => item.Labels.Contains(VersionUri.LatestLabel)) != -1);
            Debug.Assert(Versions.FindIndex(item => item.Labels.Contains(VersionUri.StableLabel)) != -1);
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            // Log and rethrow
            throw;
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
            // Log an rethrow
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

    public static List<ProductInfo> GetDefaults()
    {
        // Create an empty list of products
        List<ProductInfo> productList = new();

        // NxMeta
        var product = new ProductInfo
        {
            Product = ProductType.NxMeta
        };
        productList.Add(product);
        var versionUri = new VersionUri
        {
            Version = "5.0.0.35134",
            Uri = "https://updates.networkoptix.com/metavms/35134/linux/metavms-server-5.0.0.35134-linux_x64.deb",
            Labels = { VersionUri.StableLabel, VersionUri.LatestLabel }
        };
        product.Versions.Add(versionUri);

        // NxWitness
        product = new ProductInfo
        {
            Product = ProductType.NxWitness
        };
        productList.Add(product);
        versionUri = new VersionUri
        {
            Version = "5.0.0.35136",
            Uri = "https://updates.networkoptix.com/default/35136/linux/nxwitness-server-5.0.0.35136-linux_x64.deb",
            Labels = { VersionUri.StableLabel, VersionUri.LatestLabel }
        };
        product.Versions.Add(versionUri);

        // DWSpectrum
        product = new ProductInfo
        {
            Product = ProductType.DWSpectrum
        };
        productList.Add(product);

        // DWSpectrum Stable
        versionUri = new VersionUri
        {
            Version = "4.2.0.32842",
            Uri =
                "https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb",
            Labels = { VersionUri.StableLabel, VersionUri.LatestLabel }
        };
        product.Versions.Add(versionUri);

        return productList;
    }
}