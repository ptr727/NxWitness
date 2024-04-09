using Newtonsoft.Json;
using System.Diagnostics;
using Serilog;
using System.ComponentModel;

namespace CreateMatrix;

// https://updates.vmsproxy.com/{product}/releases.json
// https://updates.vmsproxy.com/default/releases.json
// https://updates.vmsproxy.com/metavms/releases.json
// https://updates.vmsproxy.com/digitalwatchdog/releases.json
public class ReleasesJsonSchema
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    public class Release
    {
        [JsonProperty("product")] public string Product { get; set; } = "";

        [JsonProperty("version")] public string Version { get; set; } = "";

        [JsonProperty("protocol_version")] public int ProtocolVersion { get; set; }

        [JsonProperty("publication_type")] public string PublicationType { get; set; } = "";

        [JsonProperty("release_date")] public long ReleaseDate { get; set; }

        [JsonProperty("release_delivery_days")] public int ReleaseDeliveryDays { get; set; }

        internal VersionInfo.LabelType GetLabel()
        {
            // Determine the equivalent label
            return PublicationType switch
            {
                // Use Stable or Latest based on if published or not
                ReleasePublication => IsPublished() ? VersionInfo.LabelType.Stable : VersionInfo.LabelType.Latest,
                RcPublication => VersionInfo.LabelType.RC,
                BetaPublication => VersionInfo.LabelType.Beta,
                _ => throw new InvalidEnumArgumentException($"Unknown PublicationType: {PublicationType}")
            };
        }
        internal const string ReleasePublication = "release";
        internal const string RcPublication = "rc";
        internal const string BetaPublication = "beta";
        internal const string VmsProduct = "vms";

        private bool IsPublished()
        {
            // Logic follows similar patterns as used in C++ Desktop Client
            // https://github.com/networkoptix/nx_open/blob/526967920636d3119c92a5220290ecc10957bf12/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp#L57
            // releases_info.cpp: ReleasesInfo::selectVmsRelease(), isBuildPublished(), canReceiveUnpublishedBuild()
            return ReleaseDate > 0 && ReleaseDeliveryDays >= 0;
        }
    }

    [JsonProperty("packages_urls")] public List<string> PackagesUrls { get; set; } = [];

    [JsonProperty("releases")] public List<Release> Releases { get; set; } = [];

    private static ReleasesJsonSchema FromJson(string jsonString)
    {
        var jsonSchema = JsonConvert.DeserializeObject<ReleasesJsonSchema>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    internal static List<Release> GetReleases(HttpClient httpClient, string productName)
    {
        // Load releases JSON
        // https://updates.vmsproxy.com/{product}/releases.json
        Uri releasesUri = new($"https://updates.vmsproxy.com/{productName}/releases.json");
        Log.Logger.Information("Getting release information from {Uri}", releasesUri);
        var jsonString = httpClient.GetStringAsync(releasesUri).Result;

        // Deserialize JSON
        var releasesSchema = FromJson(jsonString);
        ArgumentNullException.ThrowIfNull(releasesSchema);
        ArgumentNullException.ThrowIfNull(releasesSchema.Releases);
        Debug.Assert(releasesSchema.Releases.Count > 0);

        // Return releases
        return releasesSchema.Releases;
    }
}