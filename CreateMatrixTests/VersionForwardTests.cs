using CreateMatrix;

namespace CreateMatrixTests;

public class VersionForwardTests
{
    [Fact]
    public void VersionForward()
    {
        // Create test releases
        List<ProductInfo> oldProductList =
        [
            new ProductInfo
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                {
                    new VersionInfo { Version = "1.0", Labels = { VersionInfo.LabelType.Stable } },
                    new VersionInfo { Version = "2.0", Labels = { VersionInfo.LabelType.Latest } },
                    new VersionInfo { Version = "3.0", Labels = { VersionInfo.LabelType.RC } },
                    new VersionInfo { Version = "4.0", Labels = { VersionInfo.LabelType.Beta } },
                },
            },
        ];
        List<ProductInfo> newProductList =
        [
            new ProductInfo
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                {
                    new VersionInfo { Version = "1.1", Labels = { VersionInfo.LabelType.Stable } },
                    new VersionInfo { Version = "2.1", Labels = { VersionInfo.LabelType.Latest } },
                    new VersionInfo { Version = "3.1", Labels = { VersionInfo.LabelType.RC } },
                    new VersionInfo { Version = "4.1", Labels = { VersionInfo.LabelType.Beta } },
                },
            },
        ];

        // newProductList will be updated in-place
        // Only Stable and Latest is tested
        // Versions with multiple labels will update the version not the individual labels
        ReleaseVersionForward.Verify(oldProductList, newProductList);
        ProductInfo productInfo = newProductList.First();

        // 4 versions
        productInfo.Versions.Should().HaveCount(4);
        // 1 Latest
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest))
            .Should()
            .Be(1);
        // 1 Stable
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable))
            .Should()
            .Be(1);
        // 1 RC
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC))
            .Should()
            .Be(1);
        // 1 Beta
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta))
            .Should()
            .Be(1);
        // 1 label per version
        productInfo.Versions.Count(item => item.Labels.Count == 1).Should().Be(4);

        // Stable 1.1
        string? stableVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.Stable))
            ?.Version;
        stableVersion.Should().Be("1.1");
        // Latest 2.1
        string? latestVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.Latest))
            ?.Version;
        latestVersion.Should().Be("2.1");
        // RC 3.1
        string? rcVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.RC))
            ?.Version;
        rcVersion.Should().Be("3.1");
        // Beta 4.1
        string? betaVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.Beta))
            ?.Version;
        betaVersion.Should().Be("4.1");
    }

    [Fact]
    public void VersionRegress()
    {
        // Create test releases
        List<ProductInfo> oldProductList =
        [
            new ProductInfo
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                {
                    new VersionInfo { Version = "1.0", Labels = { VersionInfo.LabelType.Stable } },
                    new VersionInfo { Version = "2.0", Labels = { VersionInfo.LabelType.Latest } },
                    new VersionInfo { Version = "3.0", Labels = { VersionInfo.LabelType.RC } },
                    new VersionInfo { Version = "4.0", Labels = { VersionInfo.LabelType.Beta } },
                },
            },
        ];
        List<ProductInfo> newProductList =
        [
            new ProductInfo
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                {
                    new VersionInfo { Version = "0.9", Labels = { VersionInfo.LabelType.Stable } },
                    new VersionInfo { Version = "1.9", Labels = { VersionInfo.LabelType.Latest } },
                    new VersionInfo { Version = "2.9", Labels = { VersionInfo.LabelType.RC } },
                    new VersionInfo { Version = "3.9", Labels = { VersionInfo.LabelType.Beta } },
                },
            },
        ];

        // newProductList will be updated in-place
        // Only Stable and Latest is tested
        // Versions with multiple labels will update the version not the individual labels
        ReleaseVersionForward.Verify(oldProductList, newProductList);
        ProductInfo productInfo = newProductList.First();

        // 4 versions
        productInfo.Versions.Should().HaveCount(4);
        // 1 Latest
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest))
            .Should()
            .Be(1);
        // 1 Stable
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable))
            .Should()
            .Be(1);
        // 1 RC
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC))
            .Should()
            .Be(1);
        // 1 Beta
        productInfo
            .Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta))
            .Should()
            .Be(1);
        // 1 label per version
        productInfo.Versions.Count(item => item.Labels.Count == 1).Should().Be(4);

        // Stable 1.0
        string? stableVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.Stable))
            ?.Version;
        stableVersion.Should().Be("1.0");
        // Latest 2.0
        string? latestVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.Latest))
            ?.Version;
        latestVersion.Should().Be("2.0");
        // RC 3.0
        string? rcVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.RC))
            ?.Version;
        rcVersion.Should().Be("3.0");
        // Beta 4.0
        string? betaVersion = productInfo
            .Versions.FirstOrDefault(item => item.Labels.Contains(VersionInfo.LabelType.Beta))
            ?.Version;
        betaVersion.Should().Be("4.0");
    }

    [Fact]
    public void VersionRegress_FoldsOntoExistingVersion()
    {
        // Reproduces the WisenetWAVE codegen failure: the online "Latest" regressed below a
        // version that is already present as "Stable", so restoring the old "Latest" must fold
        // onto the existing entry instead of adding a duplicate version row.
        List<ProductInfo> oldProductList =
        [
            new ProductInfo
            {
                Product = ProductInfo.ProductType.WisenetWAVE,
                Versions =
                {
                    new VersionInfo
                    {
                        Version = "6.0.5.41290",
                        Labels = { VersionInfo.LabelType.Stable },
                    },
                    new VersionInfo
                    {
                        Version = "6.1.2.42921",
                        Labels = { VersionInfo.LabelType.Latest },
                    },
                },
            },
        ];
        List<ProductInfo> newProductList =
        [
            new ProductInfo
            {
                Product = ProductInfo.ProductType.WisenetWAVE,
                Versions =
                {
                    new VersionInfo
                    {
                        Version = "6.1.1.42624",
                        Labels = { VersionInfo.LabelType.Latest },
                    },
                    new VersionInfo
                    {
                        Version = "6.1.2.42921",
                        Labels = { VersionInfo.LabelType.Stable },
                    },
                },
            },
        ];

        ReleaseVersionForward.Verify(oldProductList, newProductList);
        ProductInfo productInfo = newProductList.First();

        // Folded to a single 6.1.2.42921 entry carrying both Stable and Latest
        productInfo.Versions.Should().ContainSingle();
        VersionInfo version = productInfo.Versions.First();
        version.Version.Should().Be("6.1.2.42921");
        version
            .Labels.Should()
            .BeEquivalentTo([VersionInfo.LabelType.Stable, VersionInfo.LabelType.Latest]);

        // No duplicate version numbers
        productInfo.Versions.Select(item => item.Version).Should().OnlyHaveUniqueItems();
        productInfo.Invoking(item => item.VerifyNoDuplicateVersions()).Should().NotThrow();
    }

    [Fact]
    public void VerifyNoDuplicateVersions_Throws()
    {
        // Two entries sharing a version number must be rejected
        ProductInfo productInfo = new()
        {
            Product = ProductInfo.ProductType.NxMeta,
            Versions =
            {
                new VersionInfo
                {
                    Version = "6.1.2.42921",
                    Labels = { VersionInfo.LabelType.Stable },
                },
                new VersionInfo
                {
                    Version = "6.1.2.42921",
                    Labels = { VersionInfo.LabelType.Latest },
                },
            },
        };

        productInfo
            .Invoking(item => item.VerifyNoDuplicateVersions())
            .Should()
            .Throw<InvalidOperationException>();
    }
}
