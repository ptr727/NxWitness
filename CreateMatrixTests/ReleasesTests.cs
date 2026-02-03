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

    private static ProductInfo CreateProductInfo(ReleasesJsonSchema releasesSchema)
    {
        // Match the logic with ProductInfo.GetVersions()
        // TODO: Refactor to reduce duplication and chance of divergence
        ProductInfo productInfo = new();
        foreach (Release release in releasesSchema.Releases)
        {
            VersionInfo versionInfo = new();
            versionInfo.SetVersion(release.Version);
            productInfo.AddLabel(versionInfo, release.GetLabel());
            productInfo.Versions.Add(versionInfo);
        }
        productInfo.VerifyLabels();

        return productInfo;
    }
}
