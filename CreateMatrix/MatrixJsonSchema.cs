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

internal sealed class LowercaseEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        string? value =
            reader.GetString() ?? throw new JsonException($"Expected {typeof(T).Name} value.");

        return !Enum.TryParse(value, true, out T result)
            ? throw new JsonException($"Invalid {typeof(T).Name} value '{value}'.")
            : result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
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
