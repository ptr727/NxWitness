using CreateMatrix;

namespace CreateMatrixTests;

public sealed class CommandLineTests
{
    [Fact]
    public void CreateOptions_UsesDefaultsForMakeCommand()
    {
        DirectoryInfo tempDirectory = CreateTempDirectory();
        try
        {
            FileInfo versionFile = new(Path.Combine(tempDirectory.FullName, "Version.json"));
            File.WriteAllText(versionFile.FullName, "{}");
            DirectoryInfo makeDirectory = new(Path.Combine(tempDirectory.FullName, "Make"));
            DirectoryInfo dockerDirectory = new(Path.Combine(tempDirectory.FullName, "Docker"));
            makeDirectory.Create();
            dockerDirectory.Create();

            CommandLine commandLine = new([
                "make",
                $"--versionpath={versionFile.FullName}",
                $"--makedirectory={makeDirectory.FullName}",
                $"--dockerdirectory={dockerDirectory.FullName}",
            ]);

            CommandLine.Options options = CommandLine.CreateOptions(commandLine.Result);

            options.VersionLabel.Should().Be(VersionInfo.LabelType.Latest);
            options.VersionPath?.FullName.Should().Be(versionFile.FullName);
            options.MakeDirectory?.FullName.Should().Be(makeDirectory.FullName);
            options.DockerDirectory?.FullName.Should().Be(dockerDirectory.FullName);
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void CreateOptions_ParsesExplicitVersionLabel()
    {
        DirectoryInfo tempDirectory = CreateTempDirectory();
        try
        {
            FileInfo versionFile = new(Path.Combine(tempDirectory.FullName, "Version.json"));
            File.WriteAllText(versionFile.FullName, "{}");
            DirectoryInfo makeDirectory = new(Path.Combine(tempDirectory.FullName, "Make"));
            DirectoryInfo dockerDirectory = new(Path.Combine(tempDirectory.FullName, "Docker"));
            makeDirectory.Create();
            dockerDirectory.Create();

            CommandLine commandLine = new([
                "make",
                $"--versionpath={versionFile.FullName}",
                $"--makedirectory={makeDirectory.FullName}",
                $"--dockerdirectory={dockerDirectory.FullName}",
                "--versionlabel=Beta",
            ]);

            CommandLine.Options options = CommandLine.CreateOptions(commandLine.Result);

            options.VersionLabel.Should().Be(VersionInfo.LabelType.Beta);
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void BypassStartup_ReturnsTrueOnHelp()
    {
        CommandLine commandLine = new(["--help"]);

        bool bypass = CommandLine.BypassStartup(commandLine.Result);

        bypass.Should().BeTrue();
    }

    [Fact]
    public void BypassStartup_ReturnsTrueOnParseErrors()
    {
        CommandLine commandLine = new([
            "matrix",
            "--versionpath=missing.json",
            "--matrixpath=matrix.json",
        ]);

        bool bypass = CommandLine.BypassStartup(commandLine.Result);

        bypass.Should().BeTrue();
    }

    private static DirectoryInfo CreateTempDirectory()
    {
        string directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CreateMatrixTests",
            Guid.NewGuid().ToString("N")
        );
        DirectoryInfo directoryInfo = Directory.CreateDirectory(directoryPath);
        return directoryInfo;
    }

    private static void DeleteTempDirectory(DirectoryInfo directoryInfo)
    {
        if (!directoryInfo.Exists)
        {
            return;
        }

        directoryInfo.Delete(true);
    }
}
