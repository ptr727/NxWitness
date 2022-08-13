using Serilog;
using System.Collections.Immutable;
using System.Diagnostics;

namespace CreateMatrix;

public class ImageInfo
{
    private static readonly string[] BaseNames = { "", "LSIO" };
    public string Name { get; private set; } = "";
    public string Branch { get; private set; } = "";
    public List<string> Tags { get; } = new();
    public List<string> Args { get; } = new();

    private void SetName(ProductInfo.ProductType productType, string baseName)
    {
        // E.g. NxMeta, NxMeta-LSIO
        Name = string.IsNullOrEmpty(baseName) ? productType.ToString() : $"{productType.ToString()}-{baseName}";
    }

    private void AddArgs(VersionUri versionUri)
    {
        Args.Add($"DOWNLOAD_VERSION={versionUri.Version}");
        Args.Add($"DOWNLOAD_URL={versionUri.Uri}");
    }

    private void AddTag(string tag, string? tagPrefix = null)
    {
        // E.g. latest, develop-latest
        var prefixTag = string.IsNullOrEmpty(tagPrefix) ? tag : $"{tagPrefix}-{tag}";

        // Docker Hub
        Tags.Add($"docker.io/ptr727/{Name.ToLower()}:{prefixTag}");

        // GitHub Container Registry
        Tags.Add($"ghcr.io/ptr727/{Name.ToLower()}:{prefixTag}");
    }

    public static List<ImageInfo> CreateImages(List<ProductInfo> productList)
    {
        // Iterate through products and base names and create images
        List<ImageInfo> imageList = new();
        foreach (var productInfo in productList)
            foreach (var baseName in BaseNames)
                imageList.AddRange(CreateImages(productInfo, baseName));

        // Set branch as "main" on all images
        imageList.ForEach(item => item.Branch = "main");

        // Create develop builds of NxMeta
        List<ImageInfo> developList = new();
        var nxMeta = productList.Find(item => item.Product == ProductInfo.ProductType.NxMeta);
        Debug.Assert(nxMeta != default(ProductInfo));
        foreach (var baseName in BaseNames)
            developList.AddRange(CreateImages(nxMeta, baseName, "develop"));

        // Set branch as "develop"
        developList.ForEach(item => item.Branch = "develop");

        // Add images to list
        imageList.AddRange(developList);

        // Sort args and tags to make diffs easier
        imageList.ForEach(item => { item.Args.Sort(); item.Tags.Sort(); } );

        return imageList;
    }

    private static IEnumerable<ImageInfo> CreateImages(ProductInfo productInfo, string baseName,
        string? tagPrefix = null)
    {
        // Create a set by unique versions
        var versionSet = productInfo.Versions.ToImmutableSortedSet(new VersionUriComparer());
        Debug.Assert(versionSet.Count == productInfo.Versions.Count);
        
        // Create images for each version
        List<ImageInfo> imageList = new();
        foreach (var versionUri in versionSet)
        {
            // Create image
            ImageInfo imageInfo = new();
            imageInfo.SetName(productInfo.Product, baseName);

            // Add a tag for the version
            imageInfo.AddTag(versionUri.Version, tagPrefix);

            // Add tags for all labels
            versionUri.Labels.ForEach(item => imageInfo.AddTag(item, tagPrefix));

            // Add prefix as a standalone tag when the label is latest
            if (!string.IsNullOrEmpty(tagPrefix) &&
                versionUri.Labels.FindIndex(item => item.Contains(VersionUri.LatestLabel)) != -1)
                imageInfo.AddTag(tagPrefix);

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
        Log.Logger.Information("Name: {Name}, Branch: {Branch}, Tags: {Tags}, Args: {Args}", Name, Branch, Tags.Count, Args.Count);
        Tags.ForEach(item => Log.Logger.Information("Name: {Name}, Tag: {Tag}", Name, item));
        Args.ForEach(item => Log.Logger.Information("Name: {Name}, Arg: {Arg}", Name, item));
    }
}