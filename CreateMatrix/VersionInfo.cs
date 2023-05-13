using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CreateMatrix;

public class VersionInfo : ICloneable, IComparable
{
    // Serialize enums as strings
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LabelType 
    {
        None,
        stable,
        latest,
        beta,
        rc
    }

    public string Version { get; set; } = "";
    public string Uri { get; set; } = "";
    public List<LabelType> Labels { get; set; } = new();

    object ICloneable.Clone()
    {
        return Clone();
    }
    
    int IComparable.CompareTo(object? obj)
    {
        if (obj == null)
            return 1;
        if (obj is not VersionInfo other)
            throw new InvalidCastException(nameof(obj));
        return CompareTo(other);
    }

    public VersionInfo Clone()
    {
        return (VersionInfo)MemberwiseClone();
    }

    [JsonIgnore]
    public string CleanVersion => GetCleanVersion(Version);

    public void SetCleanVersion(string version)
    {
        Version = GetCleanVersion(version);
    }

    [JsonIgnore]
    public int BuildNumber => GetBuildNumber(Version);

    public int CompareTo(VersionInfo other)
    {
        // Compare version numbers
        var thisVersion = new Version(Version);
        var otherVersion = new Version(other.Version);
        return thisVersion.CompareTo(otherVersion);
    }

    private static string GetCleanVersion(string version)
    {
        // Remove Rxx from version string
        // "5.0.0.35134 R10" -> "5.0.0.35134"
        var spaceIndex = version.IndexOf(' ');
        return spaceIndex == -1 ? version : version[..spaceIndex];
    }

    private static int GetBuildNumber(string version)
    {
        // Extract the build number using the Version class (vs. regex)
        // 5.0.0.35271 -> 35271
        // 5.1.0.35151 R1 -> 35151
        var versionClass = new Version(GetCleanVersion(version));
        return versionClass.Revision;
    }

    public static List<LabelType> GetLabelTypes()
    {
        // Create list of product types
        var labelList = new List<LabelType>();
        foreach (LabelType labelType in Enum.GetValues(typeof(LabelType)))
        {
            // Exclude None type
            if (labelType != LabelType.None)
            {
                labelList.Add(labelType);
            }
        }
        return labelList;
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
            _ => y == null ? 1 : x.CompareTo(y)
        };
    }
}