using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Serilog;

namespace CreateMatrix;

public class ImageInfo
{
    // JSON serialized must be public get and set

    public string Name { get; set; } = "";
    public string Branch { get; set; } = "";
    public string CacheScope { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public List<string> Args { get; set; } = [];

    private static readonly string[] s_baseNames = ["", "LSIO"];
    private static readonly string[] s_registryNames = ["docker.io/ptr727"];

    private void SetName(ProductInfo.ProductType productType, string baseName)
    {
        // E.g. NxMeta, NxMeta-LSIO
        Name = string.IsNullOrEmpty(baseName)
            ? productType.ToString()
            : $"{productType}-{baseName}";

        // E.g. default, lsio
        CacheScope = string.IsNullOrEmpty(baseName)
            ? "default"
            : $"{baseName.ToLower(CultureInfo.InvariantCulture)}";
    }

    private void AddArgs(VersionInfo versionInfo)
    {
        Args.Add($"{DownloadVersion}={versionInfo.Version}");
        Args.Add($"{DownloadX64Url}={versionInfo.UriX64}");
        Args.Add($"{DownloadArm64Url}={versionInfo.UriArm64}");
    }

    private void AddTag(string tag, string? tagPrefix = null)
    {
        // E.g. latest, develop-latest
        string prefixTag = string.IsNullOrEmpty(tagPrefix) ? tag : $"{tagPrefix}-{tag}";

        // E.g. "docker.io/ptr727", "ghcr.io/ptr727"
        foreach (string registry in s_registryNames)
        {
            Tags.Add(
                $"{registry}/{Name.ToLower(CultureInfo.InvariantCulture)}:{prefixTag.ToLower(CultureInfo.InvariantCulture)}"
            );
        }
    }

    public static List<ImageInfo> CreateImages(List<ProductInfo> productList)
    {
        // Create images for all products
        List<ImageInfo> imageList = [];
        foreach (ProductInfo productInfo in productList)
        {
            foreach (string baseName in s_baseNames)
            {
                imageList.AddRange(CreateImages(productInfo, baseName));
            }
        }

        // Set branch as "main" on all images
        imageList.ForEach(item => item.Branch = "main");

        // Create develop tagged images for all products
        List<ImageInfo> developList = [];
        foreach (ProductInfo productInfo in productList)
        {
            foreach (string baseName in s_baseNames)
            {
                developList.AddRange(CreateImages(productInfo, baseName, "develop"));
            }
        }

        // Set branch as "develop"
        developList.ForEach(item => item.Branch = "develop");

        // Add images to list
        imageList.AddRange(developList);

        // Sort args and tags to make diffs easier
        imageList.ForEach(item =>
        {
            item.Args.Sort();
            item.Tags.Sort();
        });

        return imageList;
    }

    private static List<ImageInfo> CreateImages(
        ProductInfo productInfo,
        string baseName,
        string? tagPrefix = null
    )
    {
        // Create a set by unique versions
        ImmutableSortedSet<VersionInfo> versionSet = productInfo.Versions.ToImmutableSortedSet(
            new VersionInfoComparer()
        );
        Debug.Assert(versionSet.Count == productInfo.Versions.Count);

        // Create images for each version
        List<ImageInfo> imageList = [];
        foreach (VersionInfo? versionUri in versionSet)
        {
            // Create image
            ImageInfo imageInfo = new();
            imageInfo.SetName(productInfo.Product, baseName);

            // Add a tag for the version
            imageInfo.AddTag(versionUri.Version, tagPrefix);

            // Add tags for all labels
            versionUri.Labels.ForEach(item => imageInfo.AddTag(item.ToString(), tagPrefix));

            // Add prefix as a standalone tag for latest
            if (
                !string.IsNullOrEmpty(tagPrefix)
                && versionUri.Labels.Contains(VersionInfo.LabelType.Latest)
            )
            {
                imageInfo.AddTag(tagPrefix);
            }

            // Add args
            imageInfo.AddArgs(versionUri);

            // Add image to list
            imageList.Add(imageInfo);
        }

        // Done
        return imageList;
    }

    public void LogInformation()
    {
        Log.Logger.Information(
            "Name: {Name}, Branch: {Branch}, Tags: {Tags}, Args: {Args}",
            Name,
            Branch,
            Tags.Count,
            Args.Count
        );
        Tags.ForEach(item => Log.Logger.Information("Name: {Name}, Tag: {Tag}", Name, item));
        Args.ForEach(item => Log.Logger.Information("Name: {Name}, Arg: {Arg}", Name, item));
    }

    public const string DownloadVersion = "DOWNLOAD_VERSION";
    public const string DownloadX64Url = "DOWNLOAD_X64_URL";
    public const string DownloadArm64Url = "DOWNLOAD_ARM64_URL";
}
