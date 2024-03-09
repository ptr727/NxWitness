using System.Diagnostics;
using Newtonsoft.Json;
using Serilog;

namespace CreateMatrix;

// https://updates.networkoptix.com/{product}/{build}/packages.json
// https://updates.networkoptix.com/metavms/35134/packages.json
// https://updates.networkoptix.com/default/35270/packages.json
// https://updates.networkoptix.com/digitalwatchdog/35271/packages.json
public class PackagesJsonSchema
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    public class Variant
    {
        [JsonProperty("name")] public string Name { get; set; } = "";

        [JsonProperty("minimumVersion")] public string MinimumVersion { get; set; } = "";
    }

    public class Package
    {
        [JsonProperty("component")] public string Component { get; set; } = "";

        [JsonProperty("platform")] public string PlatformName { get; set; } = "";

        [JsonProperty("file")] public string File { get; set; } = "";

        [JsonProperty("size")] public long Size { get; set; }

        [JsonProperty("md5")] public string Md5 { get; set; } = "";

        [JsonProperty("signature")] public string Signature { get; set; } = "";

        [JsonProperty("variants")] public List<Variant> Variants { get; set; } = [];

        internal bool IsX64Server()
        {
            // Test for Server and x64 and Ubuntu
            return Component.Equals("server", StringComparison.OrdinalIgnoreCase) && 
                PlatformName.Equals("linux_x64", StringComparison.OrdinalIgnoreCase) &&
                Variants.Any(variant => variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase));
        }

        internal bool IsArm64Server()
        {
            // Test for Server and Arm64 and Ubuntu
            return Component.Equals("server", StringComparison.OrdinalIgnoreCase) &&
                PlatformName.Equals("linux_arm64", StringComparison.OrdinalIgnoreCase) &&
                Variants.Any(variant => variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase));
        }
    }

    [JsonProperty("version")] public string Version { get; set; } = "";

    [JsonProperty("cloudHost")] public string CloudHost { get; set; } = "";

    [JsonProperty("releaseNotesUrl")] public string ReleaseNotesUrl { get; set; } = "";

    [JsonProperty("description")] public string Description { get; set; } = "";

    [JsonProperty("eula")] public string Eula { get; set; } = "";

    [JsonProperty("eulaVersion")] public long EulaVersion { get; set; }

    [JsonProperty("packages")] public List<Package> Packages { get; set; } = [];

    private static PackagesJsonSchema FromJson(string jsonString)
    {
        var jsonSchema = JsonConvert.DeserializeObject<PackagesJsonSchema>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    internal static List<Package> GetPackages(HttpClient httpClient, string productName, int buildNumber)
    {
        // Load packages JSON
        // https://updates.networkoptix.com/{product}/{build}/packages.json
        Uri packagesUri = new($"https://updates.networkoptix.com/{productName}/{buildNumber}/packages.json");
        Log.Logger.Information("Getting package information from {Uri}", packagesUri);
        var jsonString = httpClient.GetStringAsync(packagesUri).Result;

        // Deserialize JSON
        var packagesSchema = FromJson(jsonString);
        ArgumentNullException.ThrowIfNull(packagesSchema);
        ArgumentNullException.ThrowIfNull(packagesSchema.Packages);
        Debug.Assert(packagesSchema.Packages.Count > 0);

        // Return packages
        return packagesSchema.Packages;
    }
}