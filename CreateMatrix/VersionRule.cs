using Serilog;

namespace CreateMatrix;

internal class VersionRule
{
    private ProductInfo.ProductType Product { get; init; } = ProductInfo.ProductType.None;
    private VersionInfo.LabelType Label { get; init; } = VersionInfo.LabelType.None;
    private string MinVersion { get; init; } = "";

    public static readonly List<VersionRule> DefaultRuleList = new()
    { 
        // Use None to match any product or label
        new VersionRule { Label = VersionInfo.LabelType.Stable, MinVersion = "5.0" },
        new VersionRule { Label = VersionInfo.LabelType.Latest, MinVersion = "5.0" },
        new VersionRule { Label = VersionInfo.LabelType.RC, MinVersion = "5.1" },
        new VersionRule { Label = VersionInfo.LabelType.Beta, MinVersion = "5.1" }
    };

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
        if (CompareVersion(versionInfo.Version, MinVersion) >= 0)
            return true;
        
        Log.Logger.Warning("{Product}:{Label} : {Version} is less than {MinVersion}", productInfo.Product, Label, versionInfo.Version, MinVersion);
        return false;
    }

    public static bool Evaluate(List<ProductInfo> productList, List<VersionRule> ruleList, bool filterLabels)
    {
        // All products
        var result = true;
        foreach (var productInfo in productList)
        {
            // All versions
            foreach (var versionInfo in productInfo.Versions)
            {
                // Labels to remove
                var removeLabels = new List<VersionInfo.LabelType>();

                // All labels
                foreach (var label in from label in versionInfo.Labels from 
                             versionRule in ruleList.Where(versionRule => !versionRule.Evaluate(productInfo, versionInfo, label)) select label)
                {
                    removeLabels.Add(label);
                    result = false;
                }

                // Remove labels
                if (filterLabels)
                { 
                    versionInfo.Labels.RemoveAll(item => removeLabels.Contains(item));
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
                ruleList.AddRange(versionInfo.Labels.Select(label => new VersionRule { Product = productInfo.Product, Label = label, MinVersion = versionInfo.Version }));
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