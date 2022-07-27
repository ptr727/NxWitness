namespace CreateMatrix;

public class ImageInfo
{
    private static readonly string[] BaseNames = { "", "LSIO" };
    public string Name { get; private set; } = "";
    public string Branch { get; private set; } = "";
    public List<string> Tags { get; } = new();
    public List<string> Args { get; } = new();

    public static List<ImageInfo> CreateImages(List<ProductInfo> productList)
    {
        // Iterate through products and base names and create images
        List<ImageInfo> imageList = new();
        foreach (var productInfo in productList)
            foreach (var baseName in BaseNames)
                imageList.AddRange(CreateImages(productInfo, baseName, ""));

        // Set branch as main
        imageList.ForEach(item => item.Branch = "main");

        // Create develop builds of NxMeta
        List<ImageInfo> developList = new();
        var nxMeta = productList.Find(item => item.Product == ProductInfo.ProductType.NxMeta);
        ArgumentNullException.ThrowIfNull(nxMeta);

        // Iterate through base names and create images
        foreach (var baseName in BaseNames)
            developList.AddRange(CreateImages(nxMeta, baseName, "develop"));

        // Set branch as develop
        developList.ForEach(item => item.Branch = "develop");

        // Add to list
        imageList.AddRange(developList);

        return imageList;
    }

    private static IEnumerable<ImageInfo> CreateImages(ProductInfo productInfo, string baseName, string tagPrefix)
    {
        List<ImageInfo> imageList = new();

        // Create latest image
        ImageInfo imageInfo = new();
        imageInfo.SetName(productInfo.Product, baseName);

        // Latest tags
        imageInfo.AddTag("latest", tagPrefix);
        imageInfo.AddTag(productInfo.Latest.Version, tagPrefix);

        // If a prefix is set add it as a primary tag to the latest image
        // E.g. develop-latest, develop
        if (!string.IsNullOrEmpty(tagPrefix))
            imageInfo.AddTag(tagPrefix, "");

        // Latest args
        imageInfo.AddArgs(productInfo.Latest);

        // If stable and latest version is the same combine the tags
        if (productInfo.Latest.Version.Equals(productInfo.Stable.Version))
        {
            // Add stable tag to same image
            imageInfo.AddTag("stable", tagPrefix);

            // Add combined image
            imageList.Add(imageInfo);

            // Done
            return imageList;
        }
        // Latest and stable are different versions

        // Add latest image
        imageList.Add(imageInfo);

        // Crate stable image
        imageInfo = new ImageInfo();
        imageInfo.SetName(productInfo.Product, baseName);

        // Stable args
        imageInfo.AddArgs(productInfo.Stable);

        // Stable tags
        imageInfo.AddTag("stable", tagPrefix);
        imageInfo.AddTag(productInfo.Stable.Version, tagPrefix);

        // Add stable image
        imageList.Add(imageInfo);

        // Done
        return imageList;
    }

    private void SetName(ProductInfo.ProductType productType, string baseName)
    {
        // E.g. NxMeta, NxMeta-LSIO
        Name = string.IsNullOrEmpty(baseName) ? productType.ToString() : $"{productType.ToString()}-{baseName}";
    }

    private void AddArgs(ProductInfo.VersionUri versionUri)
    {
        Args.Add($"DOWNLOAD_VERSION={versionUri.Version}");
        Args.Add($"DOWNLOAD_URL={versionUri.Uri}");
    }

    private void AddTag(string tag, string tagPrefix)
    {
        // E.g. latest, develop-latest
        var prefixTag = string.IsNullOrEmpty(tagPrefix) ? tag : $"{tagPrefix}-{tag}";

        // Docker Hub
        Tags.Add($"docker.io/ptr727/{Name.ToLower()}:{prefixTag}");

        // GitHub Container Registry
        Tags.Add($"ghcr.io/ptr727/{Name.ToLower()}:{prefixTag}");
    }
}