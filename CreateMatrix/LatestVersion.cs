using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace CreateMatrix;

public static class LatestVersion
{
    public static void GetVersion(ProductType productType, out VersionUri versionUri)
    {
        switch (productType)
        {
            case ProductType.NxMeta:
                GetVersion(new Uri("https://meta.nxvms.com/api/utils/downloads"), out versionUri);
                break;
            case ProductType.NxWitness:
                GetVersion(new Uri("https://nxvms.com/api/utils/downloads"), out versionUri);
                break;
            case ProductType.DWSpectrum:
                GetVersion(new Uri("https://dwspectrum.digital-watchdog.com/api/utils/downloads"), out versionUri);
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(productType));
        }
    }

    private static void GetVersion(Uri jsonUri, out VersionUri versionUri)
    {
        versionUri = new VersionUri();
        try
        {
            // https://meta.nxvms.com/api/utils/downloads
            // https://nxvms.com/api/utils/downloads
            // https://dwspectrum.digital-watchdog.com/api/utils/downloads
            Log.Logger.Information("Loading build information JSON from {Uri}", jsonUri);
            using HttpClient httpClient = new();
            var jsonString = httpClient.GetStringAsync(jsonUri).Result;
            var jsonSchema = ReleaseJsonSchema.FromJson(jsonString);

            // "version": "5.0.0.35134 R10",
            // "version": "4.2.0.32842",
            Debug.Assert(!string.IsNullOrEmpty(jsonSchema.Version));
            versionUri.Version = jsonSchema.Version;

            // Remove Rxx from version string
            // "5.0.0.35134 R10"
            var spaceIndex = versionUri.Version.IndexOf(' ');
            if (spaceIndex != -1)
            {
                versionUri.Version = versionUri.Version[..spaceIndex];
            }

            // "product": "metavms",
            // "product": "nxwitness",
            // "product": "dwspectrum",
            Debug.Assert(!string.IsNullOrEmpty(jsonSchema.Product));
            var product = jsonSchema.Product;

            // NxWitness is 404 when using nxwitness, use default
            if (product.Equals("nxwitness", StringComparison.OrdinalIgnoreCase))
            {
                product = "default";
            }

            // DWSpectrum is 404 when using dwspectrum, use digitalwatchdog
            if (product.Equals("dwspectrum", StringComparison.OrdinalIgnoreCase))
            {
                product = "digitalwatchdog";
            }

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
                item.Platform.StartsWith("linux", StringComparison.OrdinalIgnoreCase) &&
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
            throw;
        }
    }
}