﻿using System.Text.Json.Serialization;

namespace CreateMatrix;

public class VersionInfo
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LabelType 
    {
        None,
        Stable,
        Latest,
        Beta,
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
        var version = new Version(Version);
        return version.Revision;
    }

    public void SetVersion(string version)
    {
        // Remove Rxx from version string
        // "5.0.0.35134 R10" -> "5.0.0.35134"
        var spaceIndex = version.IndexOf(' ');
        Version = spaceIndex == -1 ? version : version[..spaceIndex];
    }

    public int CompareTo(VersionInfo rhs)
    {
        return Compare(this, rhs);
    }

    public int CompareTo(string rhs)
    {
        return Compare(Version, rhs);
    }

    public static int Compare(string lhs, string rhs)
    {
        // Compare version numbers using Version class
        var lhsVersion = new Version(lhs);
        var rhsVersion = new Version(rhs);
        return lhsVersion.CompareTo(rhsVersion);
    }

    public static int Compare(VersionInfo lhs, VersionInfo rhs) 
    {
        return Compare(lhs.Version, rhs.Version);
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
            _ => y == null ? 1 : VersionInfo.Compare(x, y)
        };
    }
}
