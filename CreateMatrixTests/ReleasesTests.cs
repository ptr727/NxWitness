using CreateMatrix;

namespace CreateMatrixTests;

public class ReleasesTests
{
    [Fact]
    public void MatchLabels()
    {
        // Create test releases
        var releasesSchema = new ReleasesJsonSchema
        {
            Releases = [
                // Stable, published and released
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, ReleaseDate = 1, ReleaseDeliveryDays = 1, Version = "1.0" },
                // Latest, published not released
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, Version = "2.0" },
                // RC
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.RcPublication, Version = "3.0" },
                // Beta
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.BetaPublication, Version = "4.0" }
                ]
        };
        
        // Create ProductInfo from schema
        var productInfo = CreateProductInfo(releasesSchema);

        // 4 versions
        Assert.Equal(4, productInfo.Versions.Count);
        // 1 Latest
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)));
        // 1 Stable
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)));
        // 1 RC
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC)));
        // 1 Beta
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta)));
        // 1 label per version
        Assert.Equal(4, productInfo.Versions.Count(item => item.Labels.Count == 1));
    }

    [Fact]
    public void MissingLatest()
    {
        // Similar to MissingStable()

        // Create test releases
        var releasesSchema = new ReleasesJsonSchema
        {
            Releases = [
                // Stable, published and released
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, ReleaseDate = 1, ReleaseDeliveryDays = 1, Version = "1.0" },
                // RC
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.RcPublication, Version = "3.0" },
                // Beta
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.BetaPublication, Version = "4.0" }
                ]
        };

        // Create ProductInfo from schema
        var productInfo = CreateProductInfo(releasesSchema);

        // 3 versions
        Assert.Equal(3, productInfo.Versions.Count);
        // 1 Latest
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)));
        // 1 Stable
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)));
        // 1 RC
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC)));
        // 1 Beta
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta)));
        
        // Select all Latest or Stable labels
        var latestVersions = productInfo.Versions.Where(item => (item.Labels.Contains(VersionInfo.LabelType.Latest) || item.Labels.Contains(VersionInfo.LabelType.Stable)));
        // Should just be 1 entry
        Assert.Single(latestVersions);
        // Should have Latest and Stable labels
        var version = latestVersions.First();
        Assert.Equal(2, version.Labels.Count);
    }

    [Fact]
    public void MissingStable()
    {
        // Similar to MissingLatest()

        // Create test releases
        var releasesSchema = new ReleasesJsonSchema
        {
            Releases = [
                // Latest, published not released
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, Version = "1.0" },
                // RC
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.RcPublication, Version = "3.0" },
                // Beta
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.BetaPublication, Version = "4.0" }
                ]
        };

        // Create ProductInfo from schema
        var productInfo = CreateProductInfo(releasesSchema);

        // 3 versions
        Assert.Equal(3, productInfo.Versions.Count);
        // 1 Latest
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)));
        // 1 Stable
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)));
        // 1 RC
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC)));
        // 1 Beta
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta)));

        // Select all Latest or Stable labels
        var latestVersions = productInfo.Versions.Where(item => (item.Labels.Contains(VersionInfo.LabelType.Latest) || item.Labels.Contains(VersionInfo.LabelType.Stable)));
        // Should just be 1 entry
        Assert.Single(latestVersions);
        // Should have Latest and Stable labels
        var version = latestVersions.First();
        Assert.Equal(2, version.Labels.Count);
    }

    [Fact]
    public void MultipleReleases()
    {
        // Create test releases
        var releasesSchema = new ReleasesJsonSchema
        {
            Releases = [
                // Published not released
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, Version = "2.0" },
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, Version = "3.0" },
                new ReleasesJsonSchema.Release { PublicationType = ReleasesJsonSchema.Release.ReleasePublication, Version = "4.0" },
                ]
        };

        // Create ProductInfo from schema
        var productInfo = CreateProductInfo(releasesSchema);

        // 1 version
        Assert.Single(productInfo.Versions);
        // 2 labels per version
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Count == 2));
        // 1 Latest
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)));
        // 1 Stable
        Assert.Equal(1, productInfo.Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)));

        // Select all Latest or Stable labels
        var latestVersions = productInfo.Versions.Where(item => (item.Labels.Contains(VersionInfo.LabelType.Latest) || item.Labels.Contains(VersionInfo.LabelType.Stable)));
        // Should just be 1 entry
        Assert.Single(latestVersions);
        // Should have Latest and Stable labels
        var version = latestVersions.First();
        Assert.Equal(2, version.Labels.Count);

        // Should be the v4.0 version
        Assert.Equal("4.0", version.Version);
    }

    private static ProductInfo CreateProductInfo(ReleasesJsonSchema releasesSchema)
    {
        // Match the logic with ProductInfo.GetVersions()
        // TODO: Refactor to reduce duplication and chance of divergence
        ProductInfo productInfo = new();
        foreach (var release in releasesSchema.Releases)
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