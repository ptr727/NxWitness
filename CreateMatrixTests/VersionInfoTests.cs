using CreateMatrix;

namespace CreateMatrixTests;

public sealed class VersionInfoTests
{
    [Fact]
    public void SetVersion_RemovesReleaseSuffix()
    {
        VersionInfo versionInfo = new();

        versionInfo.SetVersion("5.1.0.35151 R1");

        versionInfo.Version.Should().Be("5.1.0.35151");
    }

    [Fact]
    public void BuildNumber_ExtractsRevision()
    {
        VersionInfo versionInfo = new() { Version = "5.0.0.35271" };

        int buildNumber = versionInfo.GetBuildNumber();

        buildNumber.Should().Be(35271);
    }
}
