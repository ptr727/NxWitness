namespace CreateMatrix;

public static class ReleaseVersionForward
{
    public static void Verify(
        IReadOnlyList<ProductInfo> oldProductList,
        IList<ProductInfo> newProductList
    )
    {
        ArgumentNullException.ThrowIfNull(oldProductList);
        ArgumentNullException.ThrowIfNull(newProductList);

        // newProductList will be updated in-place

        // Verify against all products in the new list
        foreach (ProductInfo newProduct in newProductList)
        {
            // Find matching old product, skip if not present
            ProductInfo? oldProduct = oldProductList.FirstOrDefault(item =>
                item.Product == newProduct.Product
            );
            if (oldProduct == default(ProductInfo))
            {
                Log.Logger.Warning(
                    "{Product}: Old product not found, skipping version forward checks",
                    newProduct.Product
                );
                continue;
            }

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
        VersionInfo? oldVersion = oldProduct.Versions.FirstOrDefault(item =>
            item.Labels.Contains(label)
        );
        if (oldVersion == default(VersionInfo))
        {
            Log.Logger.Warning("{Product}:{Label} : Label not found", oldProduct.Product, label);
            return;
        }

        // Find label in new product, skip if not present
        VersionInfo? newVersion = newProduct.Versions.FirstOrDefault(item =>
            item.Labels.Contains(label)
        );
        if (newVersion == default(VersionInfo))
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
            foreach (VersionInfo version in oldProduct.Versions)
            {
                newProduct.Versions.Add(version);
            }
        }
    }
}
