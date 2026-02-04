namespace CreateMatrix;

// https://updates.vmsproxy.com/{release}/releases.json
// https://updates.vmsproxy.com/default/releases.json
// https://updates.vmsproxy.com/metavms/releases.json
// https://updates.vmsproxy.com/digitalwatchdog/releases.json
// https://updates.vmsproxy.com/hanwha/releases.json

internal class Release
{
    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("publication_type")]
    public string PublicationType { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public long? ReleaseDate { get; set; }

    [JsonPropertyName("release_delivery_days")]
    public long? ReleaseDeliveryDays { get; set; }

    public VersionInfo.LabelType GetLabel() =>
        // Determine the equivalent label
        PublicationType switch
        {
            // Use Stable or Latest based on if published or not
            ReleasePublication => IsPublished()
                ? VersionInfo.LabelType.Stable
                : VersionInfo.LabelType.Latest,
            RcPublication => VersionInfo.LabelType.RC,
            BetaPublication => VersionInfo.LabelType.Beta,
            _ => throw new InvalidEnumArgumentException(
                $"Unknown PublicationType: {PublicationType}"
            ),
        };

    public const string ReleasePublication = "release";
    public const string RcPublication = "rc";
    public const string BetaPublication = "beta";
    public const string VmsProduct = "vms";
    public const string DesktopProduct = "desktop_client";

    private bool IsPublished() =>
        // Logic follows similar patterns as used in C++ Desktop Client
        // https://github.com/networkoptix/nx_open/blob/526967920636d3119c92a5220290ecc10957bf12/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp#L57
        // releases_info.cpp: ReleasesInfo::selectVmsRelease(), isBuildPublished(), canReceiveUnpublishedBuild()
        ReleaseDate > 0
        && ReleaseDeliveryDays >= 0;
}

internal class ReleasesJsonSchema
{
    [JsonPropertyName("releases")]
    public List<Release> Releases { get; init; } = [];

    private static ReleasesJsonSchema FromJson(string json)
    {
        ReleasesJsonSchema? jsonSchema = JsonSerializer.Deserialize(
            json,
            ReleasesJsonContext.Default.ReleasesJsonSchema
        );
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    public static async Task<List<Release>> GetReleasesAsync(
        string productName,
        CancellationToken cancellationToken
    )
    {
        // Load releases JSON
        // https://updates.vmsproxy.com/{product}/releases.json
        ArgumentNullException.ThrowIfNull(productName);

        HttpClient httpClient = HttpClientFactory.GetHttpClient();
        Uri releasesUri = new($"https://updates.vmsproxy.com/{productName}/releases.json");
        Log.Logger.Information("Getting release information from {Uri}", releasesUri);
        string jsonString = await httpClient
            .GetStringAsync(releasesUri, cancellationToken)
            .ConfigureAwait(false);

        // Deserialize JSON
        ReleasesJsonSchema releasesSchema = FromJson(jsonString);
        ArgumentNullException.ThrowIfNull(releasesSchema);
        ArgumentOutOfRangeException.ThrowIfZero(releasesSchema.Releases.Count);

        // Return releases
        return releasesSchema.Releases;
    }
}

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    ReadCommentHandling = JsonCommentHandling.Skip,
    UseStringEnumConverter = true,
    WriteIndented = true,
    NewLine = "\r\n"
)]
[JsonSerializable(typeof(ReleasesJsonSchema))]
internal partial class ReleasesJsonContext : JsonSerializerContext;
