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
            new()
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                [
                    new VersionInfo { Version = "1.0", Labels = [VersionInfo.LabelType.Stable] },
                    new VersionInfo { Version = "2.0", Labels = [VersionInfo.LabelType.Latest] },
                    new VersionInfo { Version = "3.0", Labels = [VersionInfo.LabelType.RC] },
                    new VersionInfo { Version = "4.0", Labels = [VersionInfo.LabelType.Beta] }
                ]
            }
        ];
        List<ProductInfo> newProductList =
        [
            new()
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                [
                    new VersionInfo { Version = "1.1", Labels = [VersionInfo.LabelType.Stable] },
                    new VersionInfo { Version = "2.1", Labels = [VersionInfo.LabelType.Latest] },
                    new VersionInfo { Version = "3.1", Labels = [VersionInfo.LabelType.RC] },
                    new VersionInfo { Version = "4.1", Labels = [VersionInfo.LabelType.Beta] }
                ]
            }
        ];

        // newProductList will be updated in-place
        // Only Stable and Latest is tested
        // Versions with multiple labels will update the version not the individual labels
        ReleaseVersionForward.Verify(oldProductList, newProductList);
        ProductInfo productInfo = newProductList.First();

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

        // Stable 1.1
        Assert.Equal("1.1", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Stable))?.Version);
        // Latest 2.1
        Assert.Equal("2.1", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Latest))?.Version);
        // RC 3.1
        Assert.Equal("3.1", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.RC))?.Version);
        // Beta 4.1
        Assert.Equal("4.1", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Beta))?.Version);
    }

    [Fact]
    public void VersionRegress()
    {
        // Create test releases
        List<ProductInfo> oldProductList =
        [
            new()
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                [
                    new VersionInfo { Version = "1.0", Labels = [VersionInfo.LabelType.Stable] },
                    new VersionInfo { Version = "2.0", Labels = [VersionInfo.LabelType.Latest] },
                    new VersionInfo { Version = "3.0", Labels = [VersionInfo.LabelType.RC] },
                    new VersionInfo { Version = "4.0", Labels = [VersionInfo.LabelType.Beta] }
                ]
            }
        ];
        List<ProductInfo> newProductList =
        [
            new()
            {
                Product = ProductInfo.ProductType.NxMeta,
                Versions =
                [
                    new VersionInfo { Version = "0.9", Labels = [VersionInfo.LabelType.Stable] },
                    new VersionInfo { Version = "1.9", Labels = [VersionInfo.LabelType.Latest] },
                    new VersionInfo { Version = "2.9", Labels = [VersionInfo.LabelType.RC] },
                    new VersionInfo { Version = "3.9", Labels = [VersionInfo.LabelType.Beta] }
                ]
            }
        ];

        // newProductList will be updated in-place
        // Only Stable and Latest is tested
        // Versions with multiple labels will update the version not the individual labels
        ReleaseVersionForward.Verify(oldProductList, newProductList);
        ProductInfo productInfo = newProductList.First();

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

        // Stable 1.0
        Assert.Equal("1.0", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Stable))?.Version);
        // Latest 2.0
        Assert.Equal("2.0", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Latest))?.Version);
        // RC 3.0
        Assert.Equal("3.0", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.RC))?.Version);
        // Beta 4.0
        Assert.Equal("4.0", productInfo.Versions.Find(item => item.Labels.Contains(VersionInfo.LabelType.Beta))?.Version);
    }
}
