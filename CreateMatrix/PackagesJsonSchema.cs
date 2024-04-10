using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace CreateMatrix;

// https://updates.networkoptix.com/{product}/{build}/packages.json
// https://updates.networkoptix.com/metavms/35134/packages.json
// https://updates.networkoptix.com/default/35270/packages.json
// https://updates.networkoptix.com/digitalwatchdog/35271/packages.json

public class Variant
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class Package
{
    [JsonPropertyName("component")]
    public string Component { get; set; } = "";

    [JsonPropertyName("platform")]
    public string PlatformName { get; set; } = "";

    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("variants")]
    public List<Variant> Variants { get; set; } = [];

    public bool IsX64Server()
    {
        // Test for Server and x64 and Ubuntu
        return Component.Equals("server", StringComparison.OrdinalIgnoreCase) &&
            PlatformName.Equals("linux_x64", StringComparison.OrdinalIgnoreCase) &&
            Variants.Any(variant => variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase));
    }

    public bool IsArm64Server()
    {
        // Test for Server and Arm64 and Ubuntu
        return Component.Equals("server", StringComparison.OrdinalIgnoreCase) &&
            PlatformName.Equals("linux_arm64", StringComparison.OrdinalIgnoreCase) &&
            Variants.Any(variant => variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase));
    }
}

public class PackagesJsonSchema
{
    [JsonPropertyName("packages")]
    public List<Package> Packages { get; set; } = [];

    private static PackagesJsonSchema FromJson(string jsonString)
    {
        var jsonSchema = JsonSerializer.Deserialize<PackagesJsonSchema>(jsonString, MatrixJsonSchema.JsonReadOptions);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    public static List<Package> GetPackages(HttpClient httpClient, string productName, int buildNumber)
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