using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using Serilog;

namespace CreateMatrix;

public class VersionJsonSchemaBase
{
    protected const string SchemaUri =
        "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/JSON/Version.schema.json";

    // Schema reference
    [JsonProperty(PropertyName = "$schema", Order = -3)]
    public string Schema { get; } = SchemaUri;

    // Default to 0 if no value specified, and always write the version first
    [DefaultValue(0)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, Order = -2)]
    public int SchemaVersion { get; set; } = VersionJsonSchema.Version;
}

public class VersionJsonSchema : VersionJsonSchemaBase
{
    public const int Version = 1;

    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    [Required] public List<ProductInfo> Products { get; set; } = new();

    public static VersionJsonSchema FromFile(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void ToFile(string path, VersionJsonSchema jsonSchema)
    {
        jsonSchema.SchemaVersion = Version;
        File.WriteAllText(path, ToJson(jsonSchema));
    }

    private static string ToJson(VersionJsonSchema jsonSchema)
    {
        return JsonConvert.SerializeObject(jsonSchema, Settings);
    }

    private static VersionJsonSchema FromJson(string jsonString)
    {
        var versionJsonSchemaBase = JsonConvert.DeserializeObject<VersionJsonSchemaBase>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(versionJsonSchemaBase);

        // Deserialize the correct version
        var schemaVersion = versionJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            // Current version
            case Version:
                var schema = JsonConvert.DeserializeObject<VersionJsonSchema>(jsonString, Settings);
                ArgumentNullException.ThrowIfNull(schema);
                return schema;
            // Unknown version
            default:
                throw new InvalidEnumArgumentException($"Unknown SchemaVersion: {schemaVersion}");
        }
    }

    public static void GenerateSchema(string path)
    {
        var generator = new JSchemaGenerator
        {
            DefaultRequired = Required.Default
        };
        var schema = generator.Generate(typeof(VersionJsonSchema));
        schema.Title = "CreateMatrix Version Schema";
        schema.SchemaVersion = new Uri("https://json-schema.org/draft-06/schema");
        schema.Id = new Uri(SchemaUri);
        File.WriteAllText(path, schema.ToString());
    }
}