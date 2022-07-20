using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using Serilog;

namespace CreateMatrix;

public enum ProductType 
{ 
    NxMeta, 
    NxWitness, 
    // ReSharper disable once InconsistentNaming
    DWSpectrum 
}

public class VersionUri
{
    public string Version { get; set; } = "";
    public string Uri { get; set; } = "";
}

public class ProductVersion
{
    public string Name { get; set; } = "";
    public VersionUri Stable { get; set; } = new();
    public VersionUri Latest { get; set; } = new();
}

public class VersionJsonSchemaBase
{
    // TODO: How to set the $schema through e.g. attributes on the class?
    // https://stackoverflow.com/questions/71625019/how-to-inject-the-json-schema-value-during-newtonsoft-jsonconvert-serializeobje
    // Schema reference
    [JsonProperty(PropertyName = "$schema", Order = -3)]
    public string Schema { get; } = SchemaUri;

    // Default to 0 if no value specified, and always write the version first
    [DefaultValue(0)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, Order = -2)]
    public int SchemaVersion { get; set; } = VersionJsonSchema.Version;

    public const string SchemaUri = "https://raw.githubusercontent.com/ptr727/NxWitness/main/CreateMatrix/Version.schema.json";
}

public class VersionJsonSchema : VersionJsonSchemaBase
{
    public const int Version = 1;

    [Required]
    public List<ProductVersion> Products { get; set; } = new ();

    public void SetDefaults()
    {
        Products = new List<ProductVersion>
        { 
            new() { 
                Name = nameof(ProductType.NxMeta), 
                Stable = new VersionUri
                { 
                    Version = "4.2.0.33313", 
                    Uri = @"https://updates.networkoptix.com/metavms/4.2.0.33313%20P2/linux/metavms-server-4.2.0.33313-linux64-patch.deb"
                },
                Latest = new VersionUri
                {
                    Version = "5.0.0.35062",
                    Uri = @"https://updates.networkoptix.com/metavms/5.0.0.35062%20R9/linux/metavms-server-5.0.0.35062-linux_x64.deb"
                }
            },
            new () {
                Name = nameof(ProductType.NxWitness),
                Stable = new VersionUri
                {
                    Version = "4.2.0.34860",
                    Uri = @"https://updates.networkoptix.com/default/4.2.0.34860/linux/nxwitness-server-4.2.0.34860-linux64-patch.deb"
                },
                Latest = new VersionUri
                {
                    Version = "5.0.0.35064",
                    Uri = @"http://updates.networkoptix.com/default/5.0.0.35064/linux/nxwitness-server-5.0.0.35064-linux_x64.deb"
                }
            },
            new () {
                Name = nameof(ProductType.DWSpectrum),
                Stable = new VersionUri
                {
                    Version = "4.2.0.32842",
                    Uri = @"https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb"
                },
                Latest = new VersionUri
                {
                    Version = "4.2.0.32842",
                    Uri = @"https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb"
                }
            }
        };
    }

    public void GetOnlineVersions()
    {
        foreach (ProductType productType in Enum.GetValues(typeof(ProductType)))
        {
            // Load version from JSON
            var productVersion = Products.First(item => item.Name.Equals(productType.ToString()));
            Log.Logger.Information("Getting latest version of {ProductType}", productType);
            LatestVersion.GetVersion(productType, out var versionUri);

            // Set stable version if different
            if (productVersion.Stable.Version.Equals(versionUri.Version)) continue;
            Log.Logger.Information("Version Updated: Old Version: {OldVersion}, New Version: {NewVersion}, New Uri: {NewUri}", productVersion.Stable.Version, versionUri.Version, versionUri.Uri);
            productVersion.Stable = versionUri;
        }
    }

    public static void WriteDefaultsToFile(string path)
    {
        Log.Logger.Information("Writing defaults to {Path}", path);
        VersionJsonSchema config = new();
        config.SetDefaults();
        ToFile(path, config);
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

    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        // We expect containers to be cleared before deserializing
        // Make sure that collections are not read-only (get; set;) else deserialized values will be appended
        // https://stackoverflow.com/questions/35482896/clear-collections-before-adding-items-when-populating-existing-objects
        ObjectCreationHandling = ObjectCreationHandling.Replace
        // TODO: Add TraceWriter to log to Serilog
    };

    public static void GenerateSchema(string path)
    {
        Log.Logger.Information("Writing schema to {Path}", path);

        // Create JSON schema
        var generator = new JSchemaGenerator
        {
            // TODO: How can I make the default schema required, and just mark individual items as not required?
            DefaultRequired = Required.Default
        };
        var schema = generator.Generate(typeof(VersionJsonSchema));
        schema.Title = "CreateMatrix Version Schema";
        schema.SchemaVersion = new Uri("http://json-schema.org/draft-06/schema");
        schema.Id = new Uri(SchemaUri);
        File.WriteAllText(path, schema.ToString());
    }
}
