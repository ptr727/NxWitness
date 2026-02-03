namespace CreateMatrix;

internal class MatrixJsonSchemaBase
{
    [JsonRequired]
    [JsonPropertyOrder(-2)]
    public int SchemaVersion { get; set; } = MatrixJsonSchema.Version;
}

internal class MatrixJsonSchema : MatrixJsonSchemaBase
{
    public const int Version = 2;

    [JsonRequired]
    public List<ImageInfo> Images { get; init; } = [];

    public static MatrixJsonSchema FromFile(string path) => FromJson(File.ReadAllText(path));

    public static void ToFile(string path, MatrixJsonSchema json)
    {
        json.SchemaVersion = Version;
        File.WriteAllText(path, ToJson(json));
    }

    private static string ToJson(MatrixJsonSchema json) =>
        JsonSerializer.Serialize(json, MatrixJsonContext.Default.MatrixJsonSchema);

    private static MatrixJsonSchema FromJson(string json)
    {
        MatrixJsonSchemaBase? jsonSchemaBase = JsonSerializer.Deserialize(
            json,
            MatrixJsonContext.Default.MatrixJsonSchemaBase
        );
        ArgumentNullException.ThrowIfNull(jsonSchemaBase);

        // Deserialize the correct version
        int schemaVersion = jsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            // Current version
            case Version:
                MatrixJsonSchema? jsonSchema = JsonSerializer.Deserialize(
                    json,
                    MatrixJsonContext.Default.MatrixJsonSchema
                );
                ArgumentNullException.ThrowIfNull(jsonSchema);
                return jsonSchema;
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
[JsonSerializable(typeof(MatrixJsonSchema))]
[JsonSerializable(typeof(MatrixJsonSchemaBase))]
internal partial class MatrixJsonContext : JsonSerializerContext;
