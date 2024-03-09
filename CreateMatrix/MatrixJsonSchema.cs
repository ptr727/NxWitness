using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace CreateMatrix;

public class MatrixJsonSchemaBase
{
    protected const string SchemaUri = "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/JSON/Matrix.schema.json";

    // Schema reference
    [JsonProperty(PropertyName = "$schema", Order = -3)]
    public string Schema { get; } = SchemaUri;

    // Default to 0 if no value specified, and always write the version first
    [DefaultValue(0)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, Order = -2)]
    public int SchemaVersion { get; set; } = MatrixJsonSchema.Version;
}

public class MatrixJsonSchema : MatrixJsonSchemaBase
{
    public const int Version = 2;

    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    [Required]
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
        return JsonConvert.SerializeObject(jsonSchema, Settings);
    }

    private static MatrixJsonSchema FromJson(string jsonString)
    {
        var matrixJsonSchemaBase = JsonConvert.DeserializeObject<MatrixJsonSchemaBase>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(matrixJsonSchemaBase);

        // Deserialize the correct version
        var schemaVersion = matrixJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            // Current version
            case Version:
                var schema = JsonConvert.DeserializeObject<MatrixJsonSchema>(jsonString, Settings);
                ArgumentNullException.ThrowIfNull(schema);
                return schema;
            case 1:
                // VersionInfo::Uri was replaced with UriX64 and UriArm64 was added
                // Breaking change, UriArm64 is required in ARM64 docker builds
                throw new InvalidEnumArgumentException($"Unsupported SchemaVersion: {schemaVersion}");
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
        var schema = generator.Generate(typeof(MatrixJsonSchema));
        schema.Title = "CreateMatrix Matrix Schema";
        schema.SchemaVersion = new Uri("https://json-schema.org/draft-06/schema");
        schema.Id = new Uri(SchemaUri);
        File.WriteAllText(path, schema.ToString());
    }
}