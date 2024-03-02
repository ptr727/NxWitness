using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace CreateMatrix;

public class VersionInfo
{
    // JSON serialized must be public get and set

    // Serialize enums as strings
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LabelType 
    {
        None,
        Stable,
        Latest,
        Beta,
        // ReSharper disable once InconsistentNaming
        RC
    }

    public string Version { get; set; } = "";
    public string UriX64 { get; set; } = "";
    public string UriArm64 { get; set; } = "";
    public List<LabelType> Labels { get; set; } = [];

    public int GetBuildNumber()
    {
        // Extract the build number using the Version class (vs. regex)
        // 5.0.0.35271 -> 35271
        // 5.1.0.35151 R1 -> 35151
        var versionClass = new Version(Version);
        return versionClass.Revision;
    }

    public void SetVersion(string version)
    {
        // Remove Rxx from version string
        // "5.0.0.35134 R10" -> "5.0.0.35134"
        var spaceIndex = version.IndexOf(' ');
        Version = spaceIndex == -1 ? version : version[..spaceIndex];
    }

    public static IEnumerable<LabelType> GetLabelTypes()
    {
        // Create list of label types
        return Enum.GetValues(typeof(LabelType)).Cast<LabelType>().Where(labelType => labelType != LabelType.None).ToList();
    }
}

public class VersionInfoComparer : Comparer<VersionInfo>
{
    // Compare using version numbers
    public override int Compare(VersionInfo? x, VersionInfo? y)
    {
        return x switch
        {
            null when y == null => 0,
            null => -1,
            _ => y == null ? 1 : VersionRule.CompareVersion(x.Version, y.Version)
        };
    }
}
