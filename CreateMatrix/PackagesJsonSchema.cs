namespace CreateMatrix;

// https://updates.networkoptix.com/{product}/{build}/packages.json
// https://updates.networkoptix.com/metavms/35134/packages.json
// https://updates.networkoptix.com/default/35270/packages.json
// https://updates.networkoptix.com/digitalwatchdog/35271/packages.json

internal sealed class Variant
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

internal sealed class Package
{
    [JsonPropertyName("component")]
    public string Component { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string PlatformName { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("variants")]
    public List<Variant> Variants { get; set; } = [];

    public bool IsX64Server() =>
        // Test for Server and x64 and Ubuntu
        Component.Equals("server", StringComparison.OrdinalIgnoreCase)
        && PlatformName.Equals("linux_x64", StringComparison.OrdinalIgnoreCase)
        && Variants.Any(variant =>
            variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase)
        );

    public bool IsArm64Server() =>
        // Test for Server and Arm64 and Ubuntu
        Component.Equals("server", StringComparison.OrdinalIgnoreCase)
        && PlatformName.Equals("linux_arm64", StringComparison.OrdinalIgnoreCase)
        && Variants.Any(variant =>
            variant.Name.Equals("ubuntu", StringComparison.OrdinalIgnoreCase)
        );
}

internal sealed class PackagesJsonSchema
{
    [JsonPropertyName("packages")]
    public List<Package> Packages { get; set; } = [];

    private static PackagesJsonSchema FromJson(string json)
    {
        PackagesJsonSchema? jsonSchema = JsonSerializer.Deserialize(
            json,
            PackagesJsonContext.Default.PackagesJsonSchema
        );
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    public static async Task<List<Package>> GetPackagesAsync(
        string releaseName,
        int buildNumber,
        CancellationToken cancellationToken
    )
    {
        // Load packages JSON
        // https://updates.networkoptix.com/{product}/{build}/packages.json
        ArgumentNullException.ThrowIfNull(releaseName);

        HttpClient httpClient = HttpClientFactory.GetHttpClient();
        Uri packagesUri = new(
            $"https://updates.networkoptix.com/{releaseName}/{buildNumber}/packages.json"
        );
        Log.Logger.Information("Getting package information from {Uri}", packagesUri);
        string jsonString = await httpClient
            .GetStringAsync(packagesUri, cancellationToken)
            .ConfigureAwait(false);

        // Deserialize JSON
        PackagesJsonSchema packagesSchema = FromJson(jsonString);
        ArgumentNullException.ThrowIfNull(packagesSchema);
        ArgumentOutOfRangeException.ThrowIfZero(packagesSchema.Packages.Count);

        // Return packages
        return packagesSchema.Packages;
    }
}

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    ReadCommentHandling = JsonCommentHandling.Skip,
    UseStringEnumConverter = true,
    WriteIndented = true,
    NewLine = "\r\n"
)]
[JsonSerializable(typeof(PackagesJsonSchema))]
internal partial class PackagesJsonContext : JsonSerializerContext;
