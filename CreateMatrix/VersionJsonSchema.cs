namespace CreateMatrix;

internal class VersionJsonSchemaBase
{
    [JsonRequired]
    [JsonPropertyOrder(-2)]
    public int SchemaVersion { get; set; } = VersionJsonSchema.Version;
}

internal class VersionJsonSchema : VersionJsonSchemaBase
{
    public const int Version = 2;

    [JsonRequired]
    public List<ProductInfo> Products { get; init; } = [];

    public static VersionJsonSchema FromFile(string path) => FromJson(File.ReadAllText(path));

    public static void ToFile(string path, VersionJsonSchema json)
    {
        json.SchemaVersion = Version;
        File.WriteAllText(path, ToJson(json));
    }

    private static string ToJson(VersionJsonSchema json) =>
        JsonSerializer.Serialize(json, VersionJsonContext.Default.VersionJsonSchema);

    private static VersionJsonSchema FromJson(string json)
    {
        VersionJsonSchemaBase? versionJsonSchemaBase = JsonSerializer.Deserialize(
            json,
            VersionJsonContext.Default.VersionJsonSchemaBase
        );
        ArgumentNullException.ThrowIfNull(versionJsonSchemaBase);

        // Deserialize the correct version
        int schemaVersion = versionJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            case Version:
                VersionJsonSchema? schema = JsonSerializer.Deserialize(
                    json,
                    VersionJsonContext.Default.VersionJsonSchema
                );
                ArgumentNullException.ThrowIfNull(schema);
                return schema;
            // case 1:
            // VersionInfo::Uri was replaced with UriX64 and UriArm64 was added
            // Breaking change, UriArm64 is required in ARM64 docker builds
            // Unknown version
            default:
                throw new NotImplementedException();
        }
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
[JsonSerializable(typeof(VersionJsonSchema))]
[JsonSerializable(typeof(VersionJsonSchemaBase))]
internal partial class VersionJsonContext : JsonSerializerContext;
