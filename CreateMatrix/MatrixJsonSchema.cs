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
    public List<ImageInfo> Images { get; set; } = [];

    public static void ToFile(string path, MatrixJsonSchema json)
    {
        json.SchemaVersion = Version;
        File.WriteAllText(path, ToJson(json));
    }

    private static string ToJson(MatrixJsonSchema json) =>
        JsonSerializer.Serialize(json, MatrixJsonContext.Default.MatrixJsonSchema);
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
