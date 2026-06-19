using CreateMatrix;

namespace CreateMatrixTests;

public sealed class ReleasesTests
{
    [Fact]
    public void MatchLabels()
    {
        // Create test releases
        ReleasesJsonSchema releasesSchema = new()
        {
            Releases =
            {
                // Stable, published and released
                new Release
                {
                    PublicationType = Release.ReleasePublication,
                    ReleaseDate = 1,
                    ReleaseDeliveryDays = 1,
                    Version = "1.0",
                },
                // Latest, published not released
                new Release { PublicationType = Release.ReleasePublication, Version = "2.0" },
                // RC
                new Release { PublicationType = Release.RcPublication, Version = "3.0" },
                // Beta
                new Release { PublicationType = Release.BetaPublication, Version = "4.0" },
            },
        };

        // Create ProductInfo from schema
        ProductInfo productInfo = CreateProductInfo(releasesSchema);

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
    }

    [Fact]
    public void MissingLatest()
    {
        // Similar to MissingStable()

        // Create test releases
        ReleasesJsonSchema releasesSchema = new()
        {
            Releases =
            {
                // Stable, published and released
                new Release
                {
                    PublicationType = Release.ReleasePublication,
                    ReleaseDate = 1,
                    ReleaseDeliveryDays = 1,
                    Version = "1.0",
                },
                // RC
                new Release { PublicationType = Release.RcPublication, Version = "3.0" },
                // Beta
                new Release { PublicationType = Release.BetaPublication, Version = "4.0" },
            },
        };

        // Create ProductInfo from schema
        ProductInfo productInfo = CreateProductInfo(releasesSchema);

        // 3 versions
        productInfo.Versions.Should().HaveCount(3);
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

        // Select all Latest or Stable labels
        IEnumerable<VersionInfo> latestVersions = productInfo.Versions.Where(item =>
            item.Labels.Contains(VersionInfo.LabelType.Latest)
            || item.Labels.Contains(VersionInfo.LabelType.Stable)
        );
        // Should just be 1 entry
        latestVersions.Should().ContainSingle();
        // Should have Latest and Stable labels
        VersionInfo version = latestVersions.First();
        version.Labels.Count.Should().Be(2);
    }

    [Fact]
    public void MissingStable()
    {
        // Similar to MissingLatest()

        // Create test releases
        ReleasesJsonSchema releasesSchema = new()
        {
            Releases =
            {
                // Latest, published not released
                new Release { PublicationType = Release.ReleasePublication, Version = "1.0" },
                // RC
                new Release { PublicationType = Release.RcPublication, Version = "3.0" },
                // Beta
                new Release { PublicationType = Release.BetaPublication, Version = "4.0" },
            },
        };

        // Create ProductInfo from schema
        ProductInfo productInfo = CreateProductInfo(releasesSchema);

        // 3 versions
        productInfo.Versions.Should().HaveCount(3);
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

        // Select all Latest or Stable labels
        IEnumerable<VersionInfo> latestVersions = productInfo.Versions.Where(item =>
            item.Labels.Contains(VersionInfo.LabelType.Latest)
            || item.Labels.Contains(VersionInfo.LabelType.Stable)
        );
        // Should just be 1 entry
        latestVersions.Should().ContainSingle();
        // Should have Latest and Stable labels
        VersionInfo version = latestVersions.First();
        version.Labels.Count.Should().Be(2);
    }

    [Fact]
    public void MultipleReleases()
    {
        // Create test releases
        ReleasesJsonSchema releasesSchema = new()
        {
            Releases =
            {
                // Published not released
                new Release { PublicationType = Release.ReleasePublication, Version = "2.0" },
                new Release { PublicationType = Release.ReleasePublication, Version = "3.0" },
                new Release { PublicationType = Release.ReleasePublication, Version = "4.0" },
            },
        };

        // Create ProductInfo from schema
        ProductInfo productInfo = CreateProductInfo(releasesSchema);

        // 1 version
        productInfo.Versions.Should().ContainSingle();
        // 2 labels per version
        productInfo.Versions.Count(item => item.Labels.Count == 2).Should().Be(1);
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

        // Select all Latest or Stable labels
        IEnumerable<VersionInfo> latestVersions = productInfo.Versions.Where(item =>
            item.Labels.Contains(VersionInfo.LabelType.Latest)
            || item.Labels.Contains(VersionInfo.LabelType.Stable)
        );
        // Should just be 1 entry
        latestVersions.Should().ContainSingle();
        // Should have Latest and Stable labels
        VersionInfo version = latestVersions.First();
        version.Labels.Count.Should().Be(2);

        // Should be the v4.0 version
        version.Version.Should().Be("4.0");
    }

    [Fact]
    public void ConflictingPublicationTypes_Throws()
    {
        // The same version number tagged with two different publication types is contradictory
        // vendor data and must be rejected rather than folded into our data.
        ReleasesJsonSchema releasesSchema = new()
        {
            Releases =
            {
                new Release
                {
                    PublicationType = Release.ReleasePublication,
                    ReleaseDate = 1,
                    ReleaseDeliveryDays = 1,
                    Version = "6.1.2.42921",
                },
                new Release { PublicationType = Release.BetaPublication, Version = "6.1.2.42921" },
            },
        };

        Action act = () => CreateProductInfo(releasesSchema);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DuplicatePublicationType_Folds()
    {
        // The same version number listed twice with the same publication type is a benign
        // duplicate and is folded to a single entry.
        ReleasesJsonSchema releasesSchema = new()
        {
            Releases =
            {
                new Release
                {
                    PublicationType = Release.ReleasePublication,
                    ReleaseDate = 1,
                    ReleaseDeliveryDays = 1,
                    Version = "6.1.2.42921",
                },
                new Release
                {
                    PublicationType = Release.ReleasePublication,
                    ReleaseDate = 1,
                    ReleaseDeliveryDays = 1,
                    Version = "6.1.2.42921",
                },
            },
        };

        ProductInfo productInfo = CreateProductInfo(releasesSchema);

        // Folded to a single version
        productInfo.Versions.Should().ContainSingle();
        productInfo.Versions.First().Version.Should().Be("6.1.2.42921");
    }

    private static ProductInfo CreateProductInfo(ReleasesJsonSchema releasesSchema)
    {
        // Mirror of the non-network portion of ProductInfo.FetchVersionsAsync(),
        // sharing the version and label logic via ProductInfo.CreateVersionInfo().
        ProductInfo productInfo = new();
        foreach (Release release in ReleasesJsonSchema.VerifyReleases(releasesSchema.Releases))
        {
            productInfo.Versions.Add(productInfo.CreateVersionInfo(release));
        }
        productInfo.VerifyLabels();

        return productInfo;
    }
}
