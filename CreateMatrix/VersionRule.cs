using Serilog;
using System.Diagnostics;

namespace CreateMatrix;

internal class VersionRule
{
    private ProductInfo.ProductType Product { get; init; } = ProductInfo.ProductType.None;
    private VersionInfo.LabelType Label { get; init; } = VersionInfo.LabelType.None;
    private string Version { get; init; } = "";

    public static readonly List<VersionRule> DefaultRuleList =
    [ 
        // Use None to match any product or label
        // Only v5.1 or later is supported with Ubuntu Jammy
        new VersionRule { Version = "5.1" }
    ];

    private bool Evaluate(ProductInfo productInfo, VersionInfo versionInfo, VersionInfo.LabelType label)
    {
        // Match the product
        if (Product != ProductInfo.ProductType.None && Product != productInfo.Product)
        {
            // No match
            return true;
        }

        // Match the label
        if (Label != VersionInfo.LabelType.None && Label != label)
        {
            // No match
            return true;
        }

        // Match the version
        if (CompareVersion(versionInfo.Version, Version) >= 0)
            return true;
        
        Log.Logger.Warning("{Product}:{Label} : {Version} is less than {EvalVersion}", productInfo.Product, Label, versionInfo.Version, Version);
        return false;
    }

    public static bool Filter(List<ProductInfo> productList, List<VersionRule> ruleList)
    {
        // All products
        var result = true;
        foreach (var productInfo in productList)
        {
            // Versions to remove
            var removeVersions = new List<VersionInfo>();

            // All versions
            foreach (var versionInfo in productInfo.Versions)
            {
                // Labels to remove
                var removeLabels = new List<VersionInfo.LabelType>();

                // Evaluate versions for all labels
                foreach (var label in from label in versionInfo.Labels from 
                             versionRule in ruleList.Where(versionRule => !versionRule.Evaluate(productInfo, versionInfo, label)) select label)
                {
                    // Mark the label to be removed
                    Log.Logger.Warning("{Product}:{Version}:{Label} filtered out", productInfo.Product, versionInfo.Version, label);
                    removeLabels.Add(label);
                    result = false;
                }

                // Remove filtered labels
                if (removeLabels.Count > 0)
                { 
                    versionInfo.Labels.RemoveAll(item => removeLabels.Contains(item));
                }

                // Remove the version if it has no labels
                if (versionInfo.Labels.Count == 0)
                {
                    // Mark the version to be removed
                    Log.Logger.Warning("{Product}:{Version} no labels filtered out", productInfo.Product, versionInfo.Version);
                    removeVersions.Add(versionInfo);
                    result = false;
                }
            }

            // Remove filtered versions
            if (removeVersions.Count > 0)
            { 
                productInfo.Versions.RemoveAll(item => removeVersions.Contains(item));
            }

            // We must have at least one version per product
            Debug.Assert(productInfo.Versions.Count >= 1);
        }

        // Done
        return result;
    }

    public static bool Merge(List<ProductInfo> productList, List<ProductInfo> mergeList)
    {
        Debug.Assert(productList.Count == mergeList.Count);

        // All products
        var result = true;
        foreach (var productInfo in productList)
        {
            // List of labels associated with version
            var versionAddList = new List<Tuple<VersionInfo.LabelType, VersionInfo>>();

            // All possible labels
            foreach (var label in VersionInfo.GetLabelTypes())
            {
                // Find the version associated with the label
                var versionInfo = productInfo.Versions.FirstOrDefault(item => item.Labels.Contains(label));
                if (versionInfo != null)
                    // Label is present
                    continue;
                
                // Find version with label from merge list
                var productMergeInfo = mergeList.First(item => item.Product == productInfo.Product);
                var versionMergeInfo = productMergeInfo.Versions.FirstOrDefault(item => item.Labels.Contains(label));
                if (versionMergeInfo == null)
                    // Label not in merge list
                    continue;
                
                // Add this label and version to the product
                versionAddList.Add(new Tuple<VersionInfo.LabelType, VersionInfo>(label, versionMergeInfo));
                result = false;
            }

            // Add versions
            foreach (var versionAddInfo in versionAddList) 
            {
                // Find a matching version by version number
                var versionProductInfo = productInfo.Versions.FirstOrDefault(item => CompareVersion(item.Version, versionAddInfo.Item2.Version) == 0);
                if (versionProductInfo != null)
                {
                    // Add label to existing version
                    versionProductInfo.Labels.Add(versionAddInfo.Item1);
                    Log.Logger.Warning("{Product} : Adding {Label} to {Version}", productInfo.Product, versionAddInfo.Item1, versionAddInfo.Item2.Version);
                }
                else 
                {
                    // Add a new version and label
                    productInfo.Versions.Add(versionAddInfo.Item2);
                    Log.Logger.Warning("{Product} : Adding {Label}:{Version}", productInfo.Product, versionAddInfo.Item1, versionAddInfo.Item2.Version);
                }
            }
        }

        // Done
        return result;
    }

    public static List<VersionRule> Create(List<ProductInfo> productList)
    {
        // Create rule list from product list
        var ruleList = new List<VersionRule>();

        // All products
        foreach (var productInfo in productList)
        {
            // All versions
            foreach (var versionInfo in productInfo.Versions)
            {
                // All labels
                ruleList.AddRange(versionInfo.Labels.Select(label => new VersionRule { Product = productInfo.Product, Label = label, Version = versionInfo.Version }));
            }
        }

        return ruleList;
    }

    public static int CompareVersion(string lhs, string rhs)
    {
        // Compare version numbers
        var lhsVersion = new Version(lhs);
        var rhsVersion = new Version(rhs);
        return lhsVersion.CompareTo(rhsVersion);
    }
}