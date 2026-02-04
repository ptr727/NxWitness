using CreateMatrix;

namespace CreateMatrixTests;

public sealed class ComposeFileTests
{
    [Fact]
    public void Create_WritesAllComposeFiles()
    {
        DirectoryInfo tempDirectory = CreateTempDirectory();
        try
        {
            ComposeFile.Create(tempDirectory);

            string testFile = Path.Combine(tempDirectory.FullName, "Test.yml");
            string developFile = Path.Combine(tempDirectory.FullName, "Test-develop.yml");
            string latestFile = Path.Combine(tempDirectory.FullName, "Test-latest.yml");

            bool testExists = File.Exists(testFile);
            bool developExists = File.Exists(developFile);
            bool latestExists = File.Exists(latestFile);

            testExists.Should().BeTrue();
            developExists.Should().BeTrue();
            latestExists.Should().BeTrue();
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Create_UsesLabelForTaggedImages()
    {
        DirectoryInfo tempDirectory = CreateTempDirectory();
        try
        {
            ComposeFile.Create(tempDirectory);

            string testFile = Path.Combine(tempDirectory.FullName, "Test.yml");
            string latestFile = Path.Combine(tempDirectory.FullName, "Test-latest.yml");

            string testContents = File.ReadAllText(testFile);
            string latestContents = File.ReadAllText(latestFile);

            testContents.Should().Contain("image: test_nxwitness");
            latestContents.Should().Contain("image: docker.io/ptr727/nxwitness:latest");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
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
