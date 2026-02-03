using CreateMatrix;

namespace CreateMatrixTests;

public class DockerFileTests
{
    [Fact]
    public void Create_WritesDockerfilesForAllProducts()
    {
        DirectoryInfo tempDirectory = CreateTempDirectory();
        try
        {
            List<ProductInfo> products = CreateProducts();

            DockerFile.Create(products, tempDirectory, VersionInfo.LabelType.Latest);

            foreach (ProductInfo.ProductType productType in ProductInfo.GetProductTypes())
            {
                string standardFile = Path.Combine(
                    tempDirectory.FullName,
                    $"{ProductInfo.GetDocker(productType, false)}.Dockerfile"
                );
                string lsioFile = Path.Combine(
                    tempDirectory.FullName,
                    $"{ProductInfo.GetDocker(productType, true)}.Dockerfile"
                );

                bool standardExists = File.Exists(standardFile);
                bool lsioExists = File.Exists(lsioFile);
                standardExists.Should().BeTrue();
                lsioExists.Should().BeTrue();
            }
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Create_UsesLatestWhenLabelMissing()
    {
        DirectoryInfo tempDirectory = CreateTempDirectory();
        try
        {
            List<ProductInfo> products = CreateProducts();

            DockerFile.Create(products, tempDirectory, VersionInfo.LabelType.Beta);

            string nxGoFile = Path.Combine(
                tempDirectory.FullName,
                $"{ProductInfo.GetDocker(ProductInfo.ProductType.NxGo, false)}.Dockerfile"
            );
            string nxMetaFile = Path.Combine(
                tempDirectory.FullName,
                $"{ProductInfo.GetDocker(ProductInfo.ProductType.NxMeta, false)}.Dockerfile"
            );

            string nxGoContents = File.ReadAllText(nxGoFile);
            string nxMetaContents = File.ReadAllText(nxMetaFile);

            nxGoContents.Should().Contain("ARG LABEL_VERSION=\"5.1.0.12345\"");
            nxMetaContents.Should().Contain("ARG LABEL_VERSION=\"5.2.0.23456\"");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    private static List<ProductInfo> CreateProducts()
    {
        List<ProductInfo> products = [];
        foreach (ProductInfo.ProductType productType in ProductInfo.GetProductTypes())
        {
            ProductInfo productInfo = new() { Product = productType };

            VersionInfo latest = new()
            {
                Version = "5.1.0.12345",
                UriX64 = new Uri("https://example.com/x64.deb"),
                UriArm64 = new Uri("https://example.com/arm64.deb"),
            };
            latest.Labels.Add(VersionInfo.LabelType.Latest);
            latest.Labels.Add(VersionInfo.LabelType.Stable);
            productInfo.Versions.Add(latest);

            if (productType == ProductInfo.ProductType.NxMeta)
            {
                VersionInfo beta = new()
                {
                    Version = "5.2.0.23456",
                    UriX64 = new Uri("https://example.com/x64-beta.deb"),
                    UriArm64 = new Uri("https://example.com/arm64-beta.deb"),
                };
                beta.Labels.Add(VersionInfo.LabelType.Beta);
                productInfo.Versions.Add(beta);
            }

            products.Add(productInfo);
        }

        return products;
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
