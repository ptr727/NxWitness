using Newtonsoft.Json;

namespace CreateMatrix;

public class VersionUri : ICloneable, IComparable
{
    public string Version { get; set; } = "";
    public string Uri { get; set; } = "";
    public List<string> Labels { get; set; } = new();

    public const string StableLabel = "stable";
    public const string LatestLabel = "latest";
    public const string BetaLabel = "beta";
    public const string RcLabel = "rc";

    public static readonly string[] KnownLabels = new string[] { VersionUri.StableLabel, VersionUri.LatestLabel, VersionUri.BetaLabel, VersionUri.RcLabel };

    object ICloneable.Clone()
    {
        return Clone();
    }
    
    int IComparable.CompareTo(object? obj)
    {
        if (obj == null)
            return 1;
        if (obj is not VersionUri other)
            throw new InvalidCastException(nameof(obj));
        return CompareTo(other);
    }

    public VersionUri Clone()
    {
        return (VersionUri)MemberwiseClone();
    }

    [JsonIgnore]
    public string CleanVersion => GetCleanVersion(Version);

    public void SetCleanVersion(string version)
    {
        Version = GetCleanVersion(version);
    }

    [JsonIgnore]
    public int BuildNumber => GetBuildNumber(Version);

    public int CompareTo(VersionUri other)
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
}

public class VersionUriComparer : Comparer<VersionUri>
{
    // Compare using version numbers
    public override int Compare(VersionUri? x, VersionUri? y)
    {
        return x switch
        {
            null when y == null => 0,
            null => -1,
            _ => y == null ? 1 : x.CompareTo(y)
        };
    }
}