using Serilog;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace CreateMatrix;

public class ImageInfo
{
    // JSON serialized must be public get and set

    public string Name { get; set; } = "";
    public string Branch { get; set; } = "";
    public string CacheScope { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> Args { get; set; } = new();

    private static readonly string[] BaseNames = { "", "LSIO" };
    private static readonly string[] RegistryNames = { "docker.io/ptr727" };

    private void SetName(ProductInfo.ProductType productType, string baseName)
    {
        // E.g. NxMeta, NxMeta-LSIO
        Name = string.IsNullOrEmpty(baseName) ? productType.ToString() : $"{productType.ToString()}-{baseName}";

        // E.g. default, lsio
        CacheScope = string.IsNullOrEmpty(baseName) ? "default" : $"{baseName.ToLower(CultureInfo.InvariantCulture)}";
    }

    private void AddArgs(VersionInfo versionInfo)
    {
        Args.Add($"DOWNLOAD_VERSION={versionInfo.Version}");
        Args.Add($"DOWNLOAD_URL={versionInfo.Uri}");
    }

    private void AddTag(string tag, string? tagPrefix = null)
    {
        // E.g. latest, develop-latest
        var prefixTag = string.IsNullOrEmpty(tagPrefix) ? tag : $"{tagPrefix}-{tag}";

        // E.g. "docker.io/ptr727", "ghcr.io/ptr727"
        foreach (var registry in RegistryNames)
        {
            Tags.Add($"{registry}/{Name.ToLower(CultureInfo.InvariantCulture)}:{prefixTag.ToLower(CultureInfo.InvariantCulture)}");
        }
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

    private static IEnumerable<ImageInfo> CreateImages(ProductInfo productInfo, string baseName, string? tagPrefix = null)
    {
        // Create a set by unique versions
        var versionSet = productInfo.Versions.ToImmutableSortedSet(new VersionInfoComparer());
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
            versionUri.Labels.ForEach(item => imageInfo.AddTag(item.ToString(), tagPrefix));

            // Add prefix as a standalone tag when the label contains latest
            if (!string.IsNullOrEmpty(tagPrefix) && versionUri.Labels.Contains(VersionInfo.LabelType.Latest))
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
        Log.Logger.Information("Name: {Name}, Branch: {Branch}, Tags: {Tags}, Args: {Args}", Name, Branch, Tags.Count, Args.Count);
        Tags.ForEach(item => Log.Logger.Information("Name: {Name}, Tag: {Tag}", Name, item));
        Args.ForEach(item => Log.Logger.Information("Name: {Name}, Arg: {Arg}", Name, item));
    }
}