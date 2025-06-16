using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Generation;

namespace CreateMatrix;

public class VersionJsonSchemaBase
{
    [JsonPropertyName("$schema")]
    [JsonPropertyOrder(-3)]
    public string Schema { get; } = SchemaUri;

    [JsonRequired]
    [JsonPropertyOrder(-2)]
    public int SchemaVersion { get; set; } = VersionJsonSchema.Version;

    protected const string SchemaUri =
        "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/JSON/Version.schema.json";
}

public class VersionJsonSchema : VersionJsonSchemaBase
{
    public const int Version = 2;

    [JsonRequired]
    public List<ProductInfo> Products { get; set; } = [];

    public static VersionJsonSchema FromFile(string path) => FromJson(File.ReadAllText(path));

    public static void ToFile(string path, VersionJsonSchema jsonSchema)
    {
        jsonSchema.SchemaVersion = Version;
        File.WriteAllText(path, ToJson(jsonSchema));
    }

    private static string ToJson(VersionJsonSchema jsonSchema) =>
        JsonSerializer.Serialize(jsonSchema, MatrixJsonSchema.JsonWriteOptions);

    private static VersionJsonSchema FromJson(string jsonString)
    {
        VersionJsonSchemaBase? versionJsonSchemaBase =
            JsonSerializer.Deserialize<VersionJsonSchemaBase>(
                jsonString,
                MatrixJsonSchema.JsonReadOptions
            );
        ArgumentNullException.ThrowIfNull(versionJsonSchemaBase);

        // Deserialize the correct version
        int schemaVersion = versionJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            case Version:
                VersionJsonSchema? schema = JsonSerializer.Deserialize<VersionJsonSchema>(
                    jsonString,
                    MatrixJsonSchema.JsonReadOptions
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
        JsonSchema schemaBuilder = new JsonSchemaBuilder()
            .FromType<VersionJsonSchema>(
                new SchemaGeneratorConfiguration { PropertyOrder = PropertyOrder.ByName }
            )
            .Title("CreateMatrix Version Schema")
            .Id(new Uri(SchemaUri))
            .Schema(new Uri(schemaVersion))
            .Build();
        string jsonSchema = JsonSerializer.Serialize(
            schemaBuilder,
            MatrixJsonSchema.JsonWriteOptions
        );
        File.WriteAllText(path, jsonSchema);
    }
}
