using System.Text.Json.Serialization.Metadata;

namespace CreateMatrix;

public class MatrixJsonSchemaBase
{
    [JsonPropertyName("$schema")]
    [JsonPropertyOrder(-3)]
    public string Schema { get; } = SchemaUri;

    [JsonRequired]
    [JsonPropertyOrder(-2)]
    public int SchemaVersion { get; set; } = MatrixJsonSchema.Version;

    protected const string SchemaUri =
        "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/JSON/Matrix.schema.json";
}

public class MatrixJsonSchema : MatrixJsonSchemaBase
{
    public const int Version = 2;

    [JsonRequired]
    public ICollection<ImageInfo> Images { get; init; } = [];

    public static MatrixJsonSchema FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return FromJson(File.ReadAllText(path));
    }

    public static void ToFile(string path, MatrixJsonSchema jsonSchema)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        File.WriteAllText(path, ToJson(jsonSchema));
    }

    private static string ToJson(MatrixJsonSchema jsonSchema) =>
        JsonSerializer.Serialize(jsonSchema, JsonWriteOptions);

    private static MatrixJsonSchema FromJson(string jsonString)
    {
        MatrixJsonSchemaBase? matrixJsonSchemaBase =
            JsonSerializer.Deserialize<MatrixJsonSchemaBase>(jsonString, JsonReadOptions);
        ArgumentNullException.ThrowIfNull(matrixJsonSchemaBase);

        // Deserialize the correct version
        int schemaVersion = matrixJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            // Current version
            case Version:
                MatrixJsonSchema? schema = JsonSerializer.Deserialize<MatrixJsonSchema>(
                    jsonString,
                    JsonReadOptions
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

    public static void GenerateSchema(string path)
    {
        const string schemaVersion = "https://json-schema.org/draft/2020-12/schema";
        JsonNode schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(
            CreateMatrixJsonContext.Default.MatrixJsonSchema,
            JsonSchemaExporterOptions.Default
        );
        JsonObject schemaObject = schemaNode.AsObject();
        schemaObject["$schema"] = schemaVersion;
        schemaObject["$id"] = SchemaUri;
        schemaObject["title"] = "CreateMatrix Matrix Schema";
        File.WriteAllText(
            path,
            schemaObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );
    }

    public static readonly JsonSerializerOptions JsonReadOptions = new(
        CreateMatrixJsonContext.Default.Options
    )
    {
        TypeInfoResolver = CreateMatrixJsonContext.Default,
    };

    public static readonly JsonSerializerOptions JsonWriteOptions = new(
        CreateMatrixJsonContext.Default.Options
    )
    {
        TypeInfoResolver = JsonTypeInfoResolver.WithAddedModifier(
            CreateMatrixJsonContext.Default,
            ExcludeObsoletePropertiesModifier
        ),
    };

    private static void ExcludeObsoletePropertiesModifier(JsonTypeInfo typeInfo)
    {
        // Only process objects
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        // Iterate over all properties
        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            // Do not serialize [Obsolete] items
            if (property.AttributeProvider?.IsDefined(typeof(ObsoleteAttribute), true) == true)
            {
                property.ShouldSerialize = (_, _) => false;
            }
        }
    }
}

/// <summary>
/// Source-generation context for CreateMatrix JSON serialization.
/// </summary>
[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default,
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    ReadCommentHandling = JsonCommentHandling.Skip,
    UseStringEnumConverter = true,
    WriteIndented = true
)]
[JsonSerializable(typeof(MatrixJsonSchema))]
[JsonSerializable(typeof(MatrixJsonSchemaBase))]
[JsonSerializable(typeof(VersionJsonSchema))]
[JsonSerializable(typeof(VersionJsonSchemaBase))]
[JsonSerializable(typeof(ReleasesJsonSchema))]
[JsonSerializable(typeof(PackagesJsonSchema))]
[JsonSerializable(typeof(Release))]
[JsonSerializable(typeof(Package))]
[JsonSerializable(typeof(Variant))]
[JsonSerializable(typeof(ProductInfo))]
[JsonSerializable(typeof(VersionInfo))]
[JsonSerializable(typeof(ImageInfo))]
public partial class CreateMatrixJsonContext : JsonSerializerContext { }
