using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Serilog;

namespace CreateMatrix;

public class ImageInfo
{
    public void SetName(string productName, string baseName)
    {
        // E.g. NxMeta, NxMeta-LSIO
        Name = string.IsNullOrEmpty(baseName) ? productName : $"{productName}-{baseName}";
    }

    public string Name { get; set; } = "";
    public string Branch { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> Args { get; set; } = new();

    public void AddArgs(VersionUri versionUri)
    {
        Args.Add($"DOWNLOAD_VERSION={versionUri.Version}");
        Args.Add($"DOWNLOAD_URL={versionUri.Uri}");
    }

    public void AddTag(string tag, string tagPrefix)
    {
        // E.g. latest, develop-latest
        var prefixTag = string.IsNullOrEmpty(tagPrefix) ? tag : $"{tagPrefix}-{tag}";

        // Docker Hub
        Tags.Add($"docker.io/ptr727/{Name.ToLower()}:{prefixTag}");

        // GitHub Container Registry
        Tags.Add($"ghcr.io/ptr727/{Name.ToLower()}:{prefixTag}");
    }

    public static List<ImageInfo> CreateImages(ProductVersion productVersion, string baseName, string tagPrefix)
    {
        List<ImageInfo> imageList = new();

        // Create latest image
        ImageInfo imageInfo = new();
        imageInfo.SetName(productVersion.Name, baseName);

        // Latest tags
        imageInfo.AddTag("latest", tagPrefix);
        imageInfo.AddTag(productVersion.Latest.Version, tagPrefix);

        // If a prefix is set add it as a primary tag to the latest image
        // E.g. develop-latest, develop
        if (!string.IsNullOrEmpty(tagPrefix))
        {
            imageInfo.AddTag(tagPrefix, "");
        }

        // Latest args
        imageInfo.AddArgs(productVersion.Latest);

        // If stable and latest version is the same combine the tags
        if (productVersion.Latest.Version.Equals(productVersion.Stable.Version))
        {
            // Add stable tag to same image
            imageInfo.AddTag("stable", tagPrefix);

            // Add combined image
            imageList.Add(imageInfo);

            // Done
            return imageList;
        }
        // Latest and stable are different versions

        // Add latest image
        imageList.Add(imageInfo);

        // Crate stable image
        imageInfo = new ImageInfo();
        imageInfo.SetName(productVersion.Name, baseName);

        // Stable args
        imageInfo.AddArgs(productVersion.Stable);

        // Stable tags
        imageInfo.AddTag("stable", tagPrefix);
        imageInfo.AddTag(productVersion.Stable.Version, tagPrefix);

        // Add stable image
        imageList.Add(imageInfo);

        // Done
        return imageList;
    }
}

public class MatrixJsonSchemaBase
{
    // Default to 0 if no value specified, and always write the version first
    [DefaultValue(0)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, Order = -2)]
    public int SchemaVersion { get; set; } = MatrixJsonSchema.Version;
}

public class MatrixJsonSchema : MatrixJsonSchemaBase
{
    public const int Version = 1;

    [Required]
    public List<ImageInfo> Images { get; set; } = new();

    public static MatrixJsonSchema FromFile(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void ToFile(string path, MatrixJsonSchema jsonSchema)
    {
        jsonSchema.SchemaVersion = Version;
        File.WriteAllText(path, ToJson(jsonSchema));
    }

    private static string ToJson(MatrixJsonSchema jsonSchema)
    {
        return JsonConvert.SerializeObject(jsonSchema, Settings);
    }

    private static MatrixJsonSchema FromJson(string jsonString)
    {
        var versionJsonSchemaBase = JsonConvert.DeserializeObject<VersionJsonSchemaBase>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(versionJsonSchemaBase);
        var schemaVersion = versionJsonSchemaBase.SchemaVersion;
        switch (schemaVersion)
        {
            case Version:
                var schema = JsonConvert.DeserializeObject<MatrixJsonSchema>(jsonString, Settings);
                ArgumentNullException.ThrowIfNull(schema);
                return schema;
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
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };
}
