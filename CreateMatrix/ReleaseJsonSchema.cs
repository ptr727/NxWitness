using Newtonsoft.Json;

namespace CreateMatrix;

public class Installer
{
    [JsonProperty("platform")]
    public string Platform { get; set; } = "";

    [JsonProperty("appType")]
    public string AppType { get; set; } = "";

    [JsonProperty("beta")]
    public bool Beta { get; set; }

    [JsonProperty("cloudGroup")]
    public string CloudGroup { get; set; } = "";

    [JsonProperty("fileName")]
    public string FileName { get; set; } = "";

    [JsonProperty("path")]
    public string Path { get; set; } = "";

    [JsonProperty("niceName")]
    public string NiceName { get; set; } = "";
}

public class Platform
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("files")]
    public List<Installer> Files { get; set; } = new();
}

public class ReleaseJsonSchema
{
    [JsonProperty("version")]
    public string Version { get; set; } = "";

    [JsonProperty("releaseNotes")]
    public string ReleaseNotes { get; set; } = "";

    [JsonProperty("product")]
    public string Product { get; set; } = "";

    [JsonProperty("productDescription")]
    public string ProductDescription { get; set; } = "";

    [JsonProperty("date")]
    public string Date { get; set; } = "";

    [JsonProperty("buildNumber")]
    public string BuildNumber { get; set; } = "";

    [JsonProperty("password")]
    public string Password { get; set; } = "";

    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("installers")]
    public List<Installer> Installers { get; set; } = new();

    [JsonProperty("platforms")]
    public List<Platform> Platforms { get; set; } = new();

    [JsonProperty("backwardsCompatible")]
    public bool BackwardsCompatible { get; set; }

    [JsonProperty("cloudGroup")]
    public string CloudGroup { get; set; } = "";

    [JsonProperty("beta")]
    public bool Beta { get; set; }

    [JsonProperty("dismissed")]
    public bool Dismissed { get; set; }

    [JsonProperty("releaseUrl")]
    public string ReleaseUrl { get; set; } = "";

    public static string ToJson(ReleaseJsonSchema jsonSchema)
    {
        return JsonConvert.SerializeObject(jsonSchema, Settings);
    }

    public static ReleaseJsonSchema FromJson(string jsonString)
    {
        var jsonSchema = JsonConvert.DeserializeObject<ReleaseJsonSchema>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };
}