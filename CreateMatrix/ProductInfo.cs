using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using Serilog;

namespace CreateMatrix;

public class ProductInfo
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProductType
    {
        None,
        NxMeta,
        NxWitness,
        DWSpectrum,
        WisenetWAVE
    }

    public ProductType Product { get; set; }

    public List<VersionInfo> Versions { get; set; } = [];

    public string GetCompany() => GetCompany(Product);

    public string GetRelease() => GetRelease(Product);

    public string GetDescription() => GetDescription(Product);

    // Used for release JSON API https://updates.vmsproxy.com/{release}/releases.json path
    public static string GetRelease(ProductType productType)
    {
        return productType switch
        {
            ProductType.NxMeta => "metavms",
            ProductType.NxWitness => "default",
            ProductType.DWSpectrum => "digitalwatchdog",
            ProductType.WisenetWAVE => "hanwha",
            _ => throw new InvalidEnumArgumentException(nameof(Product))
        };
    }

    // Used for ${COMPANY_NAME} mediaserver install path and user account
    public static string GetCompany(ProductType productType)
    {
        return productType switch
        {
            ProductType.NxMeta => "networkoptix-metavms",
            ProductType.NxWitness => "networkoptix",
            ProductType.DWSpectrum => "digitalwatchdog",
            ProductType.WisenetWAVE => "hanwha",
            _ => throw new InvalidEnumArgumentException(nameof(Product))
        };
    }

    // Used for ${LABEL_DESCRIPTION} in Dockerfile
    public static string GetDescription(ProductType productType)
    {
        return productType switch
        {
            ProductType.NxMeta => "Nx Meta VMS",
            ProductType.NxWitness => "Nx Witness VMS",
            ProductType.DWSpectrum => "DW Spectrum IPVMS",
            ProductType.WisenetWAVE => "Wisenet WAVE VMS",
            _ => throw new InvalidEnumArgumentException(nameof(Product))
        };
    }

    public static IEnumerable<ProductType> GetProductTypes()
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
        // Match the logic with ReleasesTests.CreateProductInfo()
        // TODO: Refactor to reduce duplication and chance of divergence

        // Get version information using releases.json and package.json 
        Log.Logger.Information("{Product}: Getting online release information...", Product);
        try
        {
            // Reuse HttpClient
            using HttpClient httpClient = new();

            // Get all releases
            var releasesList = ReleasesJsonSchema.GetReleases(httpClient, GetRelease());
            foreach (var release in releasesList)
            {
                // We expect only "vms" products
                Debug.Assert(release.Product.Equals(Release.VmsProduct, StringComparison.OrdinalIgnoreCase));

                // Set version
                VersionInfo versionInfo = new();
                Debug.Assert(!string.IsNullOrEmpty(release.Version));
                versionInfo.SetVersion(release.Version);

                // Add the label
                AddLabel(versionInfo, release.GetLabel());

                // Get the build number from the version
                var buildNumber = versionInfo.GetBuildNumber();

                // Get available packages for this release
                var packageList = PackagesJsonSchema.GetPackages(httpClient, GetRelease(), buildNumber);

                // Get the x64 and arm64 server ubuntu server packages
                var packageX64 = packageList.Find(item => item.IsX64Server());
                Debug.Assert(packageX64 != default(Package));
                Debug.Assert(!string.IsNullOrEmpty(packageX64.File));
                var packageArm64 = packageList.Find(item => item.IsArm64Server());
                Debug.Assert(packageArm64 != default(Package));
                Debug.Assert(!string.IsNullOrEmpty(packageArm64.File));

                // Create the download URLs
                // https://updates.networkoptix.com/{product}/{build}/{file}
                versionInfo.UriX64 = $"https://updates.networkoptix.com/{GetRelease()}/{buildNumber}/{packageX64.File}";
                versionInfo.UriArm64 = $"https://updates.networkoptix.com/{GetRelease()}/{buildNumber}/{packageArm64.File}";

                // Verify and add to list
                if (VerifyVersion(versionInfo)) 
                    Versions.Add(versionInfo);
            }

            // Make sure all labels are correct
            VerifyLabels();
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            // Log and rethrow
            throw;
        }
    }

    private bool VerifyVersion(VersionInfo versionInfo)
    {
        // Static rules:

        // Ubuntu Jammy requires version 5.1 or later
        if (versionInfo.CompareTo("5.1") >= 0) 
            return true;
        
        Log.Logger.Warning("{Product}:{Version} : Ubuntu Jammy requires v5.1+", Product, versionInfo.Version);
        return false;
    }

    public void AddLabel(VersionInfo versionInfo, VersionInfo.LabelType label)
    {
        // Ignore if label is None
        if (label == VersionInfo.LabelType.None)
            return;

        // Does this label already exists in other versions
        var existingVersion = Versions.Find(item => item.Labels.Contains(label));
        if (existingVersion == default(VersionInfo))
        {
            // New label
            versionInfo.Labels.Add(label);
            return;
        }

        // Is this version larger than the other version
        if (versionInfo.CompareTo(existingVersion) <= 0) 
            return;
        
        Log.Logger.Warning("{Product}: Replacing {Label} from {ExistingVersion} to {NewVersion}", Product, label, existingVersion.Version, versionInfo.Version);

        // Remove from other version and add to this version
        existingVersion.Labels.Remove(label);
        versionInfo.Labels.Add(label);
    }

    private VersionInfo? FindMissingLabel(VersionInfo.LabelType targetLabel, List<VersionInfo.LabelType> sourceLabels)
    {
        foreach (var label in sourceLabels)
        {
            // Find last matching item, must be sorted
            var version = Versions.FindLast(item => item.Labels.Contains(label));
            if (version != default(VersionInfo))
            {
                Log.Logger.Warning("{Product}: Using {SourceLabel} for {TargetLabel}", Product, label, targetLabel);
                return version;
            }
        }
        return default;
    }

    public void VerifyLabels()
    {
        // Sort by version number
        Versions.Sort(new VersionInfoComparer());

        // If no Latest label is set, use Stable or RC or Beta as Latest
        if (!Versions.Any(item => item.Labels.Contains(VersionInfo.LabelType.Latest)))
        {
            var latest = FindMissingLabel(VersionInfo.LabelType.Latest, [VersionInfo.LabelType.Stable, VersionInfo.LabelType.RC, VersionInfo.LabelType.Beta]);
            Debug.Assert(latest != default(VersionInfo));
            latest.Labels.Add(VersionInfo.LabelType.Latest);
        }

        // If no Stable label is set, use Latest as stable
        if (!Versions.Any(item => item.Labels.Contains(VersionInfo.LabelType.Stable)))
        {
            var stable = FindMissingLabel(VersionInfo.LabelType.Stable, [VersionInfo.LabelType.Latest]);
            Debug.Assert(stable != default(VersionInfo));
            stable.Labels.Add(VersionInfo.LabelType.Stable);
        }

        // Remove all versions without labels
        Versions.RemoveAll(item => item.Labels.Count == 0);

        // Sort by label
        Versions.ForEach(item => item.Labels.Sort());

        // Must have 1 Latest and 1 Stable label
        Debug.Assert(Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)) == 1);
        Debug.Assert(Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)) == 1);

        // Must have no more than 1 Beta or RC labels
        Debug.Assert(Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta)) <= 1);
        Debug.Assert(Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC)) <= 1);
    }

    public void LogInformation()
    {
        foreach (var version in Versions)
            Log.Logger.Information("{Product}: Version: {Version}, Label: {Labels}, UriX64: {UriX64}, UriArm64: {UriArm64}", Product, version.Version, version.Labels, version.UriX64, version.UriArm64);
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

    public void Verify()
    {
        // Match verification logic executed during GetVersions()

        // Verify each version
        List<VersionInfo> removeVersions = [];
        foreach (var version in Versions.Where(version => !VerifyVersion(version)))
        {
            Log.Logger.Warning("{Product} : Removing {Version}", Product, version.Version);
            removeVersions.Add(version);
        }
        Versions.RemoveAll(item => removeVersions.Contains(item));

        // Verify labels
        VerifyLabels();
    }
}