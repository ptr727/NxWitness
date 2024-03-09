using Serilog;

namespace CreateMatrix;

internal static class ReleaseVersionForward
{
    internal static void Verify(List<ProductInfo> oldProductList, List<ProductInfo> newProductList)
    {
        // newProductList will be updated in-place

        // Verify all products
        foreach (var product in ProductInfo.GetProductTypes())
        {
            // Find matching products
            var oldProduct = oldProductList.First(item => item.Product == product);
            var newProduct = newProductList.First(item => item.Product == product);

            // Verify only Stable and Latest, other labels are optional
            List< VersionInfo.LabelType> labels = [ VersionInfo.LabelType.Stable, VersionInfo.LabelType.Latest ];
            foreach (var label in labels) 
                Verify(oldProduct, newProduct, label);
        }
    }

    private static void Verify(ProductInfo oldProduct, ProductInfo newProduct, VersionInfo.LabelType label)
    {
        // Find matching versions
        var oldVersion = oldProduct.Versions.First(item => item.Labels.Contains(label));
        var newVersion = newProduct.Versions.First(item => item.Labels.Contains(label));

        // New version must be >= old version
        if (oldVersion.CompareTo(newVersion) <= 0) 
            return;
        
        Log.Logger.Error("{Product}:{Label} : OldVersion: {OldVersion} > NewVersion: {NewVersion}", newProduct.Product, label, oldVersion.Version, newVersion.Version);

        // Do all the labels match
        if (oldVersion.Labels.SequenceEqual(newVersion.Labels)) 
        {
            Log.Logger.Warning("{Product}:{Label} Using OldVersion: {OldVersion} instead of NewVersion: {NewVersion}", newProduct.Product, label, oldVersion.Version, newVersion.Version);

            // Replace the new version with the old version
            newProduct.Versions.Remove(newVersion);
            newProduct.Versions.Add(oldVersion);
        }
        else
        {
            // TODO: Unwind labels to replace only specific version-label pairs
            Log.Logger.Warning("{Product}: Using old versions instead of new versions", newProduct.Product);

            // Replace all versions if the labels do not match
            newProduct.Versions.Clear();
            newProduct.Versions.AddRange(oldProduct.Versions);
        }
    }
}
