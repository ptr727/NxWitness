namespace CreateMatrix;

internal static class ReleaseVersionForward
{
    public static void Verify(List<ProductInfo> oldProductList, List<ProductInfo> newProductList)
    {
        // newProductList will be updated in-place

        // Verify against all products in the old list
        foreach (ProductInfo oldProduct in oldProductList)
        {
            // Find matching new product, must be present
            ProductInfo newProduct = newProductList.First(item =>
                item.Product == oldProduct.Product
            );

            // Verify all labels
            foreach (VersionInfo.LabelType label in VersionInfo.GetLabelTypes())
            {
                Verify(oldProduct, newProduct, label);
            }
        }
    }

    private static void Verify(
        ProductInfo oldProduct,
        ProductInfo newProduct,
        VersionInfo.LabelType label
    )
    {
        // TODO: It is possible that a label is released, then pulled, then re-released with a lesser version

        // Find label in old and new product, skip if not present
        VersionInfo? oldVersion = oldProduct.Versions.Find(item => item.Labels.Contains(label));
        if (oldVersion == null)
        {
            Log.Logger.Warning("{Product}:{Label} : Label not found", oldProduct.Product, label);
            return;
        }

        // Find label in new product, skip if not present
        VersionInfo? newVersion = newProduct.Versions.Find(item => item.Labels.Contains(label));
        if (newVersion == null)
        {
            Log.Logger.Warning("{Product}:{Label} : Label not found", newProduct.Product, label);
            return;
        }

        // New version must be >= old version
        if (oldVersion.CompareTo(newVersion) <= 0)
        {
            return;
        }

        Log.Logger.Error(
            "{Product}:{Label} : OldVersion: {OldVersion} > NewVersion: {NewVersion}",
            newProduct.Product,
            label,
            oldVersion.Version,
            newVersion.Version
        );

        // Do all the labels match
        if (oldVersion.Labels.SequenceEqual(newVersion.Labels))
        {
            Log.Logger.Warning(
                "{Product}:{Label} Using OldVersion: {OldVersion} instead of NewVersion: {NewVersion}",
                newProduct.Product,
                label,
                oldVersion.Version,
                newVersion.Version
            );

            // Replace the new version with the old version
            _ = newProduct.Versions.Remove(newVersion);
            newProduct.Versions.Add(oldVersion);
        }
        else
        {
            // TODO: Unwind labels to replace only specific version-label pairs
            Log.Logger.Warning(
                "{Product}: Using old versions instead of new versions",
                newProduct.Product
            );

            // Replace all versions if the labels do not match
            newProduct.Versions.Clear();
            newProduct.Versions.AddRange(oldProduct.Versions);
        }
    }
}
