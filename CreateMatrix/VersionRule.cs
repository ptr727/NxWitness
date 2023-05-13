using System.Diagnostics;
using Serilog;

namespace CreateMatrix;

class VersionRule
{
    ProductInfo.ProductType Product { get; set; }
    VersionInfo.LabelType Label { get; set; }
    string MinVersion { get; set; } = "";

    public bool Evaluate(ProductInfo productInfo)
    {
        Debug.Assert(!string.IsNullOrEmpty(MinVersion));
        Debug.Assert(productInfo.Product == Product);
        Debug.Assert(productInfo.Versions.FirstOrDefault(versionInfo => versionInfo.Labels.Contains(Label)) != null);

        // Iterate through all the versions in the product
        foreach (var versionInfo in productInfo.Versions)
        {
            // Match the label
            if (Label == VersionInfo.LabelType.None || versionInfo.Labels.Contains(Label))
            {
                // Compare the version
                if (CompareVersion(MinVersion, versionInfo.Version) < 0)
                {
                    Log.Logger.Error("{Product}:{Label} : {Version} is less than {MinVersion}", productInfo.Product, Label, versionInfo.Version, MinVersion);
                    return false;
                }
            }
        }

        // Done
        return true;
    }

    public static readonly VersionRule[] VersionRules = new VersionRule[] 
    { 
        // Update the rules as needed
        // https://github.com/ptr727/NxWitness/issues/62
        // Use None to match any product or label
        new VersionRule() { Product = ProductInfo.ProductType.None, Label = VersionInfo.LabelType.stable, MinVersion = "5.0" },
        new VersionRule() { Product = ProductInfo.ProductType.None, Label = VersionInfo.LabelType.latest, MinVersion = "5.0" },
        new VersionRule() { Product = ProductInfo.ProductType.None, Label = VersionInfo.LabelType.rc, MinVersion = "5.1" },
        new VersionRule() { Product = ProductInfo.ProductType.None, Label = VersionInfo.LabelType.beta, MinVersion = "5.1" },
    };

    public static int CompareVersion(string lhs, string rhs)
    {
        // Compare version numbers
        var lhsVersion = new Version(lhs);
        var rhsVersion = new Version(rhs);
        return lhsVersion.CompareTo(rhsVersion);
    }

    public static bool Evaluate(List<ProductInfo> productList)
    {
        // Iterate over all the products
        foreach (var productInfo in productList)
        {
            // Find a rule matching the product and evaluate
            var matchRule = FindMatchRule(productInfo);
            if (matchRule != null && !matchRule.Evaluate(productInfo))
            {
                return false;                    
            }
        }

        // Done
        return true;        
    }

    public static bool Evaluate(List<ProductInfo> fileProductList, List<ProductInfo> onlineProductList)
    {
        // Evaluate static rules against online products
        if (!Evaluate(onlineProductList))
        {
            return false;
        }

        // Evaluate online products against file products
        bool update = false;
        foreach (var onlineProduct in onlineProductList)
        {
            // Find the file product matching the online product
            var fileProduct = fileProductList.Find(item => item.Product == onlineProduct.Product);
            ArgumentNullException.ThrowIfNull(fileProduct);

            // Test every known label
            foreach (var labelType in VersionInfo.GetLabelTypes())
            {
                // Find the version matching the label
                var fileVersion = fileProduct.Versions.FirstOrDefault(item => item.Labels.Contains(labelType));
                var onlineVersion = onlineProduct.Versions.FirstOrDefault(item => item.Labels.Contains(labelType));

                // If either has no matching label then skip the check
                if (fileVersion == null || onlineVersion == null)
                {
                    continue;
                }

                // Compare the online version number with the file version number
                var compare = onlineVersion.CompareTo(fileVersion);
                if (compare < 0)
                {
                    // Version numbers may only increment

                    // Ignore stable version regressions
                    // https://github.com/ptr727/NxWitness/issues/62
                    if (labelType == VersionInfo.LabelType.stable)
                    {
                        Log.Logger.Warning("{Product}:{Label} : Online version {OnlineVersion} is less than file version {FileVersion}", fileProduct.Product, labelType, onlineVersion.Version, fileVersion.Version);
                    }
                    else
                    {
                        // Any other labels regressing is an error
                        Log.Logger.Error("{Product}:{Label} : Online version {OnlineVersion} is less than file version {FileVersion}", fileProduct.Product, labelType, onlineVersion.Version, fileVersion.Version);
                        return false;
                    }
                }
                else if (compare > 0)
                {
                    // Newer online version
                    Log.Logger.Information("{Product}:{Label} : Online version {OnlineVersion} is greater than file version {FileVersion}", fileProduct.Product, labelType, onlineVersion.Version, fileVersion.Version);
                    update = true;
                }
            }
        }
    }

    private static VersionRule? FindMatchRule(ProductInfo productInfo)
    {
        // Iterate through all rules
        foreach (var versionRule in VersionRules)
        {
            // Match by product type or None
            if (versionRule.Product == ProductInfo.ProductType.None || versionRule.Product == productInfo.Product)
            {
                // Match by None label
                if (versionRule.Label == VersionInfo.LabelType.None)
                {
                    // Match
                    return versionRule;
                }

                // Iterate through all versions
                foreach (var versionInfo in productInfo.Versions)
                {
                    // Match by label
                    if (versionInfo.Labels.Contains(versionRule.Label))
                    {
                        // Match
                        return versionRule;
                    }
                }
            }
        }

        // No match
        return null;
    }
}