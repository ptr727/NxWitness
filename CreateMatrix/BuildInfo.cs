using System.Security.AccessControl;

namespace CreateMatrix;

public class BuildInfo
{
    public static List<ImageInfo> CreateImages(List<ProductVersion> productList)
    {
        // Iterate through products
        List<ImageInfo> imageList = new();
        foreach (var productVersion in productList)
        {
            // Iterate through base names
            foreach (var baseName in BuildInfo.BaseNames)
            {
                // Create images
                imageList.AddRange(ImageInfo.CreateImages(productVersion, baseName, ""));
            }
        }

        // Set branch as main
        imageList.ForEach(item => item.Branch = "main");

        // Create develop builds of NxMeta
        List<ImageInfo> developList = new();
        var nxMeta = productList.Find(item => item.Name.Equals("NxMeta", StringComparison.InvariantCultureIgnoreCase));
        ArgumentNullException.ThrowIfNull(nxMeta);

        // Iterate through base names
        foreach (var baseName in BuildInfo.BaseNames)
        {
            // Create images
            developList.AddRange(ImageInfo.CreateImages(nxMeta, baseName, "develop"));
        }

        // Set branch as develop
        developList.ForEach(item => item.Branch = "develop");

        // Add to list
        imageList.AddRange(developList);

        return imageList;
    }

    public static readonly string[] BaseNames = { "", "LSIO" };
}
