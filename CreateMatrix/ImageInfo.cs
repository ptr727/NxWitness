using System.Collections.Immutable;

namespace CreateMatrix;

internal class ImageInfo
{
    public enum BranchType
    {
        None,
        Main,
        Develop,
    }

    public enum BaseType
    {
        None,
        Ubuntu,
        LSIO,
    }

    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(LowercaseEnumConverter<ProductInfo.ProductType>))]
    public ProductInfo.ProductType Product { get; set; } = ProductInfo.ProductType.None;

    [JsonConverter(typeof(LowercaseEnumConverter<BranchType>))]
    public BranchType Branch { get; set; } = BranchType.None;

    [JsonConverter(typeof(LowercaseEnumConverter<BaseType>))]
    public BaseType Base { get; set; } = BaseType.None;

    public List<string> Tags { get; set; } = [];
    public List<string> Args { get; set; } = [];

    private const string Registry = "docker.io/ptr727";

    private ImageInfo(ProductInfo.ProductType productType, BranchType branchType, BaseType baseType)
    {
        Product = productType;
        Branch = branchType;
        Base = baseType;
        Name = baseType == BaseType.Ubuntu ? productType.ToString() : $"{productType}-LSIO";
    }

    private void AddArgs(VersionInfo versionInfo)
    {
        Args.Add($"{DownloadVersion}={versionInfo.Version}");
        Args.Add($"{DownloadX64Url}={versionInfo.UriX64}");
        Args.Add($"{DownloadArm64Url}={versionInfo.UriArm64}");
    }

    private void AddTag(string tag)
    {
        if (Branch == BranchType.Develop)
        {
            tag = $"develop-{tag}";
        }
        Tags.Add($"{Registry}/{Name.ToLowerInvariant()}:{tag.ToLowerInvariant()}");
    }

    private void AddTag(VersionInfo.LabelType labelType)
    {
        string tag = labelType.ToString().ToLowerInvariant();
        if (Branch == BranchType.Develop)
        {
            tag = labelType == VersionInfo.LabelType.Latest ? $"develop" : $"develop-{tag}";
        }
        Tags.Add($"{Registry}/{Name.ToLowerInvariant()}:{tag.ToLowerInvariant()}");
    }

    public static List<ImageInfo> CreateImages(List<ProductInfo> productList)
    {
        // Create images for all products
        List<ImageInfo> imageList = [];
        productList.ForEach(product =>
        {
            imageList.AddRange(CreateImages(product, BaseType.Ubuntu, BranchType.Main));
            imageList.AddRange(CreateImages(product, BaseType.Ubuntu, BranchType.Develop));
            imageList.AddRange(CreateImages(product, BaseType.LSIO, BranchType.Main));
            imageList.AddRange(CreateImages(product, BaseType.LSIO, BranchType.Develop));
        });

        return imageList;
    }

    private static List<ImageInfo> CreateImages(
        ProductInfo productInfo,
        BaseType baseType,
        BranchType branchType
    )
    {
        // Create a set by unique versions
        ImmutableSortedSet<VersionInfo> versionSet = productInfo.Versions.ToImmutableSortedSet(
            new VersionInfoComparer()
        );
        Debug.Assert(versionSet.Count == productInfo.Versions.Count);

        // Create images for each version
        List<ImageInfo> imageList = [];
        foreach (VersionInfo version in versionSet)
        {
            // Create image
            ImageInfo imageInfo = new(productInfo.Product, branchType, baseType);

            // Add a tag for the version
            imageInfo.AddTag(version.Version);

            // Add tags for all labels
            version.Labels.ForEach(imageInfo.AddTag);

            // Add args
            imageInfo.AddArgs(version);

            // Sort args and tags to make diffs easier
            imageInfo.Args.Sort();
            imageInfo.Tags.Sort();

            // Add image to list
            imageList.Add(imageInfo);
        }

        // Done
        return imageList;
    }

    public void LogInformation()
    {
        Log.Logger.Information(
            "Name: {Name}, Base: {Base}, Branch: {Branch}, Tags: {Tags}, Args: {Args}",
            Name,
            Base,
            Branch,
            Tags.Count,
            Args.Count
        );
        Tags.ForEach(item => Log.Logger.Information("Name: {Name}, Tag: {Tag}", Name, item));
        Args.ForEach(item => Log.Logger.Information("Name: {Name}, Arg: {Arg}", Name, item));
    }

    private const string DownloadVersion = "DOWNLOAD_VERSION";
    private const string DownloadX64Url = "DOWNLOAD_X64_URL";
    private const string DownloadArm64Url = "DOWNLOAD_ARM64_URL";
}
