using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using Serilog;

namespace CreateMatrix;

public class VersionJsonSchemaBase
{
    protected const string SchemaUri =
        "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/Version.schema.json";

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

    public void SetDefaults()
    {
        Products = new List<ProductInfo>
        {
            new()
            {
                Product = ProductInfo.ProductType.NxMeta,
                Stable = new ProductInfo.VersionUri
                {
                    Version = "5.0.0.35134",
                    Uri =
                        @"https://updates.networkoptix.com/metavms/35134/linux/metavms-server-5.0.0.35134-linux_x64.deb"
                },
                Latest = new ProductInfo.VersionUri
                {
                    Version = "5.0.0.35134",
                    Uri =
                        @"https://updates.networkoptix.com/metavms/35134/linux/metavms-server-5.0.0.35134-linux_x64.deb"
                }
            },
            new()
            {
                Product = ProductInfo.ProductType.NxWitness,
                Stable = new ProductInfo.VersionUri
                {
                    Version = "5.0.0.35136",
                    Uri =
                        @"https://updates.networkoptix.com/default/35136/linux/nxwitness-server-5.0.0.35136-linux_x64.deb"
                },
                Latest = new ProductInfo.VersionUri
                {
                    Version = "5.0.0.35136",
                    Uri =
                        @"https://updates.networkoptix.com/default/35136/linux/nxwitness-server-5.0.0.35136-linux_x64.deb"
                }
            },
            new()
            {
                Product = ProductInfo.ProductType.DWSpectrum,
                Stable = new ProductInfo.VersionUri
                {
                    Version = "4.2.0.32842",
                    Uri =
                        @"https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb"
                },
                Latest = new ProductInfo.VersionUri
                {
                    Version = "4.2.0.32842",
                    Uri =
                        @"https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb"
                }
            }
        };
    }

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
                Log.Logger.Error("Unknown schema version : {SchemaVersion}", schemaVersion);
                throw new NotSupportedException(nameof(schemaVersion));
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