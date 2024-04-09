using Serilog;

namespace CreateMatrix;

internal static class ReleaseVersionForward
{
    internal static void Verify(List<ProductInfo> oldProductList, List<ProductInfo> newProductList)
    {
        // newProductList will be updated in-place

        // Verify against all products in the old list
        foreach (var oldProduct in oldProductList)
        {
            // Find matching new product, must be present
            var newProduct = newProductList.First(item => item.Product == oldProduct.Product);

            // Verify all labels
            foreach (var label in VersionInfo.GetLabelTypes()) 
                Verify(oldProduct, newProduct, label);
        }
    }

    private static void Verify(ProductInfo oldProduct, ProductInfo newProduct, VersionInfo.LabelType label)
    {
        // Find label in old product, skip if not present
        var oldVersion = oldProduct.Versions.FirstOrDefault(item => item.Labels.Contains(label));
        if (oldVersion == default(VersionInfo))
            return;

        // New product must have the same label
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
