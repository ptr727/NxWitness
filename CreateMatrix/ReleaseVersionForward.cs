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
        // NOTE: Forward-only is intentional; a published tag must not regress to a lesser version.
        // If a label is released, then pulled, then re-released with a lesser version, we keep the
        // old (higher) version. This is harmless while the old build is still downloadable, and if
        // its files were actually removed the run fails loudly in VerifyUrlsAsync (404) for a human
        // to resolve, e.g. by manually adjusting Version.json. There is no silent-corruption path.

        // Find label in old and new product, skip if not present
        VersionInfo? oldVersion = oldProduct.Versions.Find(item => item.Labels.Contains(label));
        if (oldVersion == null)
        {
            Log.Logger.Warning(
                "{Product}:{Label} : Label not found in old versions",
                oldProduct.Product,
                label
            );
            return;
        }

        // Find label in new product, skip if not present
        VersionInfo? newVersion = newProduct.Versions.Find(item => item.Labels.Contains(label));
        if (newVersion == null)
        {
            Log.Logger.Warning(
                "{Product}:{Label} : Label not found in new versions",
                newProduct.Product,
                label
            );
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

            // Remove the regressed new version
            _ = newProduct.Versions.Remove(newVersion);

            // The old version number may already be present in the new list under a different
            // label (e.g. restoring Latest onto a version that is already Stable). Fold the
            // old version's labels into that existing entry instead of adding a duplicate row,
            // otherwise two entries would share a version number.
            VersionInfo? existingVersion = newProduct.Versions.Find(item =>
                item.CompareTo(oldVersion) == 0
            );
            if (existingVersion == null)
            {
                // No existing entry, add the old version
                newProduct.Versions.Add(oldVersion);
            }
            else
            {
                // Fold the old version's labels into the existing entry
                Log.Logger.Warning(
                    "{Product}:{Label} Folding OldVersion: {OldVersion} labels into existing version",
                    newProduct.Product,
                    label,
                    oldVersion.Version
                );
                foreach (VersionInfo.LabelType oldLabel in oldVersion.Labels)
                {
                    if (!existingVersion.Labels.Contains(oldLabel))
                    {
                        existingVersion.Labels.Add(oldLabel);
                    }
                }
                existingVersion.Labels.Sort();
            }
        }
        else
        {
            // The label moved between versions that carry different label sets, so a surgical
            // per-version-label swap is ambiguous. Rather than attempting to unwind individual
            // version-label pairs, take the conservative approach and revert the whole product to
            // the last-known-good versions. This may discard newer online versions until the
            // regression clears, but it avoids producing an inconsistent label arrangement.
            Log.Logger.Warning(
                "{Product}:{Label} : Labels differ, reverting all versions to old versions",
                newProduct.Product,
                label
            );

            // Replace all versions if the labels do not match
            newProduct.Versions.Clear();
            newProduct.Versions.AddRange(oldProduct.Versions);
        }
    }
}
