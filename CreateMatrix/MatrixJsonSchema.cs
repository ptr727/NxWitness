using Json.Schema.Generation;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    protected const string SchemaUri = "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/JSON/Matrix.schema.json";
}

public class MatrixJsonSchema : MatrixJsonSchemaBase
{
    public const int Version = 2;

    [JsonRequired]
    public List<ImageInfo> Images { get; set; } = [];

    public static MatrixJsonSchema FromFile(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void ToFile(string path, MatrixJsonSchema jsonSchema)
    {
        File.WriteAllText(path, ToJson(jsonSchema));
    }

    private static string ToJson(MatrixJsonSchema jsonSchema)
    {
        return JsonSerializer.Serialize(jsonSchema, JsonWriteOptions);
    }

    private static MatrixJsonSchema FromJson(string jsonString)
    {
        var matrixJsonSchemaBase = JsonSerializer.Deserialize<MatrixJsonSchemaBase>(jsonString, JsonReadOptions);
        ArgumentNullException.ThrowIfNull(matrixJsonSchemaBase);

        // Deserialize the correct version
        var schemaVersion = matrixJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            // Current version
            case Version:
                var schema = JsonSerializer.Deserialize<MatrixJsonSchema>(jsonString, JsonReadOptions);
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
        var schemaBuilder = new JsonSchemaBuilder().FromType<MatrixJsonSchema>(new SchemaGeneratorConfiguration { PropertyOrder = PropertyOrder.ByName })
            .Title("CreateMatrix Matrix Schema")
            .Id(new Uri(SchemaUri))
            .Schema(new Uri(schemaVersion))
            .Build();
        var jsonSchema = JsonSerializer.Serialize(schemaBuilder, JsonWriteOptions);
        File.WriteAllText(path, jsonSchema);
    }

    public static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        AllowTrailingCommas = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            .WithAddedModifier(ExcludeObsoletePropertiesModifier),
        WriteIndented = true
    };

    private static void ExcludeObsoletePropertiesModifier(JsonTypeInfo typeInfo)
    {
        // Only process objects
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        // Iterate over all properties
        foreach (var property in typeInfo.Properties)
        {
            // Do not serialize [Obsolete] items
            if (property.AttributeProvider?.IsDefined(typeof(ObsoleteAttribute), true) == true)
                property.ShouldSerialize = (_, _) => false;
        }
    }
}
