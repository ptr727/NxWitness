using System.Diagnostics;
using Newtonsoft.Json;
using Serilog;

namespace CreateMatrix;

// https://{cloudportal}/api/utils/downloads
// https://meta.nxvms.com/api/utils/downloads
// https://nxvms.com/api/utils/downloads
// https://dwspectrum.digital-watchdog.com/api/utils/downloads

public class DownloadsJsonSchema
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    [JsonProperty("version")] public string Version { get; set; } = "";

    [JsonProperty("releaseNotes")] public string ReleaseNotes { get; set; } = "";

    [JsonProperty("product")] public string Product { get; set; } = "";

    [JsonProperty("productDescription")] public string ProductDescription { get; set; } = "";

    [JsonProperty("date")] public string Date { get; set; } = "";

    [JsonProperty("buildNumber")] public string BuildNumber { get; set; } = "";

    [JsonProperty("password")] public string Password { get; set; } = "";

    [JsonProperty("type")] public string Type { get; set; } = "";

    [JsonProperty("installers")] public List<Installer> Installers { get; set; } = new();

    [JsonProperty("platforms")] public List<Platform> Platforms { get; set; } = new();

    [JsonProperty("backwardsCompatible")] public bool BackwardsCompatible { get; set; }

    [JsonProperty("cloudGroup")] public string CloudGroup { get; set; } = "";

    [JsonProperty("beta")] public bool Beta { get; set; }

    [JsonProperty("dismissed")] public bool Dismissed { get; set; }

    [JsonProperty("releaseUrl")] public string ReleaseUrl { get; set; } = "";

    public static DownloadsJsonSchema FromJson(string jsonString)
    {
        var jsonSchema = JsonConvert.DeserializeObject<DownloadsJsonSchema>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    public class Installer
    {
        [JsonProperty("platform")] public string PlatformName { get; set; } = "";

        [JsonProperty("appType")] public string AppType { get; set; } = "";

        [JsonProperty("beta")] public bool Beta { get; set; }

        [JsonProperty("cloudGroup")] public string CloudGroup { get; set; } = "";

        [JsonProperty("fileName")] public string FileName { get; set; } = "";

        [JsonProperty("path")] public string Path { get; set; } = "";

        [JsonProperty("niceName")] public string NiceName { get; set; } = "";
    }

    public class Platform
    {
        [JsonProperty("name")] public string Name { get; set; } = "";

        [JsonProperty("files")] public List<Installer> Files { get; set; } = new();
    }
    
    internal static DownloadsJsonSchema GetDownloads(HttpClient httpClient, string cloudHost)
    {
        // Load downloads JSON
        // https://{cloudhost}/api/utils/downloads
        Uri downloadsUri = new($"{cloudHost}/api/utils/downloads");
        Log.Logger.Information("Getting download information from {Uri}", downloadsUri);
        var jsonString = httpClient.GetStringAsync(downloadsUri).Result;
        var downloadsSchema = FromJson(jsonString);
        ArgumentNullException.ThrowIfNull(downloadsSchema);
        ArgumentNullException.ThrowIfNull(downloadsSchema.Installers);
        Debug.Assert(downloadsSchema.Installers.Count > 0);

        // Return releases
        return downloadsSchema;
    }
}