using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace CreateMatrix;

public class ProductInfo
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProductType
    {
        NxMeta,
        NxWitness,

        // Use same spelling as used in Makefile
        // ReSharper disable once InconsistentNaming
        DWSpectrum
    }

    // Configure cloud hosts and product paths (can't use path info from JSON)
    private static readonly Dictionary<ProductType, HostInfo> CloudHosts = new()
    {
        {
            ProductType.NxMeta,
            new HostInfo { Host = "https://meta.nxvms.com", Path = "metavms" }
        },
        {
            ProductType.NxWitness,
            new HostInfo { Host = "https://nxvms.com", Path = "default" }
        },
        {
            ProductType.DWSpectrum,
            new HostInfo { Host = "https://dwspectrum.digital-watchdog.com", Path = "digitalwatchdog" }
        }
    };

    public ProductType Product { get; init; }
    public VersionUri Stable { get; set; } = new();
    public VersionUri Latest { get; set; } = new();

    public static void GetLatestVersion(IEnumerable<ProductInfo> productsEnumerable)
    {
        foreach (var productInfo in productsEnumerable)
        {
            Log.Logger.Information("Getting latest version of {ProductType}", productInfo.Product);
            GetLatestVersion(productInfo.Product, out var versionUri);

            // Did the stable version change
            if (!productInfo.Stable.Version.Equals(versionUri.Version))
            {
                // Update stable version
                Log.Logger.Warning(
                    "Stable Version Updated : Old Version: {OldVersion}, New Version: {NewVersion}, New Uri: {NewUri}",
                    productInfo.Stable.Version, versionUri.Version, versionUri.Uri);
                productInfo.Stable = versionUri;
            }

            // Is the stable version greater than the latest version
            var stableVersion = new Version(productInfo.Stable.Version);
            var latestVersion = new Version(productInfo.Latest.Version);
            if (stableVersion.CompareTo(latestVersion) > 0)
            {
                Log.Logger.Warning("Stable Version > Latest Version : Stable: {Stable}, Latest: {Latest}",
                    productInfo.Stable.Version, productInfo.Latest.Version);
                Log.Logger.Information(
                    "Latest Version Updated : Old Version: {OldVersion}, New Version: {NewVersion}, New Uri: {NewUri}",
                    productInfo.Latest.Version, productInfo.Stable.Version, productInfo.Stable.Uri);
                productInfo.Latest = productInfo.Stable;
            }
        }
    }

    private static void GetLatestVersion(ProductType productType, out VersionUri versionUri)
    {
        // TODO: Get official documentation on how to build download URL
        // https://support.networkoptix.com/hc/en-us/community/posts/7615204163607-Automating-downloaded-product-versions-using-JSON-API
        versionUri = new VersionUri();
        try
        {
            // Create update endpoint
            // https://{Cloud Portal URL}/api/utils/downloads
            // https://meta.nxvms.com/api/utils/downloads
            // https://nxvms.com/api/utils/downloads
            // https://dwspectrum.digital-watchdog.com/api/utils/downloads
            var hostInfo = CloudHosts[productType];
            Uri hostUri = new(hostInfo.Host);
            Uri updateUri = new(hostUri, "/api/utils/downloads");
            Log.Logger.Information("Loading build information JSON from {Uri}", updateUri);
            using HttpClient httpClient = new();
            var jsonString = httpClient.GetStringAsync(updateUri).Result;
            var jsonSchema = ReleaseJsonSchema.FromJson(jsonString);

            // "version": "5.0.0.35134 R10",
            // "version": "4.2.0.32842",
            Debug.Assert(!string.IsNullOrEmpty(jsonSchema.Version));
            versionUri.Version = jsonSchema.Version;

            // Remove Rxx from version string
            // "5.0.0.35134 R10"
            var spaceIndex = versionUri.Version.IndexOf(' ');
            if (spaceIndex != -1) versionUri.Version = versionUri.Version[..spaceIndex];

            // "product": "metavms",
            // "product": "nxwitness",
            // "product": "dwspectrum",
            // NxWitness is 404 when using nxwitness, use default
            // DWSpectrum is 404 when using dwspectrum, use digitalwatchdog
            // Use the static host configuration for the product
            var product = hostInfo.Path;

            // "buildNumber": "35134",
            Debug.Assert(!string.IsNullOrEmpty(jsonSchema.BuildNumber));
            var buildNumber = jsonSchema.BuildNumber;

            // "installers": [
            // "platform": "linux_x64" (v5)
            // "platform": "linux64", (v4)
            // "appType": "server"
            // "path": "linux/metavms-server-5.0.0.35134-linux_x64.deb",
            Debug.Assert(jsonSchema.Installers.Count > 0);
            var installer = jsonSchema.Installers.First(item =>
                item.PlatformName.StartsWith("linux", StringComparison.OrdinalIgnoreCase) &&
                item.AppType.Equals("server", StringComparison.OrdinalIgnoreCase));
            ArgumentNullException.ThrowIfNull(installer);

            // https://updates.networkoptix.com/metavms/35134/linux/metavms-server-5.0.0.35134-linux_x64.deb
            // http://updates.networkoptix.com/default/35136/linux/nxwitness-server-5.0.0.35136-linux_x64.deb
            // https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb
            versionUri.Uri = $"https://updates.networkoptix.com/{product}/{buildNumber}/{installer.Path}";
            Log.Logger.Information("Version: {Version}, File Name: {FileName}, Uri: {Uri}", versionUri.Version,
                installer.FileName, versionUri.Uri);

            // Verify URL
            Log.Logger.Information("Verifying Uri: {Uri}", versionUri.Uri);
            var httpResponse = httpClient.GetAsync(versionUri.Uri).Result;
            httpResponse.EnsureSuccessStatusCode();
            Log.Logger.Information("File Name: {FileName}, File Size: {FileSize}, Last Modified: {LastModified}",
                installer.FileName,
                httpResponse.Content.Headers.ContentLength,
                httpResponse.Content.Headers.LastModified);
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e, MethodBase.GetCurrentMethod()?.Name))
        {
            // Log and rethrow
            throw;
        }
    }

    private class HostInfo
    {
        public string Host { get; init; } = "";
        public string Path { get; init; } = "";
    }

    public class VersionUri
    {
        public string Version { get; set; } = "";
        public string Uri { get; set; } = "";
    }
}