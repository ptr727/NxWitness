namespace CreateMatrix;

public class ProductInfo
{
    private readonly List<VersionInfo> _versions = [];

    public enum ProductType
    {
        None,
        NxGo,
        NxMeta,
        NxWitness,
        DWSpectrum,
        WisenetWAVE,
    }

    public ProductType Product { get; set; }

    public ICollection<VersionInfo> Versions => _versions;

    public string GetCompany() => GetCompany(Product);

    public string GetRelease() => GetRelease(Product);

    public string GetDescription() => GetDescription(Product);

    public string GetDocker(bool lsio) => GetDocker(Product, lsio);

    // Used for release JSON API https://updates.vmsproxy.com/{release}/releases.json path
    public static string GetRelease(ProductType productType) =>
        productType switch
        {
            ProductType.NxGo => "nxgo",
            ProductType.NxMeta => "metavms",
            ProductType.NxWitness => "default",
            ProductType.DWSpectrum => "digitalwatchdog",
            ProductType.WisenetWAVE => "hanwha",
            ProductType.None => throw new NotImplementedException(),
            _ => throw new InvalidEnumArgumentException(nameof(Product)),
        };

    // Used for ${COMPANY_NAME} mediaserver install path and user account
    public static string GetCompany(ProductType productType) =>
        productType switch
        {
            ProductType.NxGo => "networkoptix",
            ProductType.NxMeta => "networkoptix-metavms",
            ProductType.NxWitness => "networkoptix",
            ProductType.DWSpectrum => "digitalwatchdog",
            ProductType.WisenetWAVE => "hanwha",
            ProductType.None => throw new NotImplementedException(),
            _ => throw new InvalidEnumArgumentException(nameof(Product)),
        };

    // Used for ${LABEL_DESCRIPTION} in Dockerfile
    public static string GetDescription(ProductType productType) =>
        productType switch
        {
            ProductType.NxGo => "Nx Go VMS",
            ProductType.NxMeta => "Nx Meta VMS",
            ProductType.NxWitness => "Nx Witness VMS",
            ProductType.DWSpectrum => "DW Spectrum IPVMS",
            ProductType.WisenetWAVE => "Wisenet WAVE VMS",
            ProductType.None => throw new NotImplementedException(),
            _ => throw new InvalidEnumArgumentException(nameof(Product)),
        };

    // Dockerfile name, excluding the .Dockerfile extension
    // TODO: Consolidate with ImageInfo.SetName(), e.g. add enum for Ubuntu, LSIO, etc.
    public static string GetDocker(ProductType productType, bool lsio) =>
        $"{productType}{(lsio ? "-LSIO" : "")}";

    public static IEnumerable<ProductType> GetProductTypes() =>
        // Create list of product types
        [
            .. Enum.GetValues<ProductType>()
                .Cast<ProductType>()
                .Where(productType => productType != ProductType.None),
        ];

    public static IReadOnlyList<ProductInfo> GetProducts()
    {
        // Create list of all known products
        List<ProductInfo> products =
        [
            .. from ProductType productType in GetProductTypes()
            select new ProductInfo { Product = productType },
        ];
        return products;
    }

    /// <summary>
    /// Fetches and validates version data from the release endpoints.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the request.
    /// </param>
    public async Task FetchVersionsAsync(CancellationToken cancellationToken)
    {
        // Match the logic with ReleasesTests.CreateProductInfo()
        // TODO: Refactor to reduce duplication and chance of divergence

        // Get version information using releases.json and package.json
        Log.Logger.Information("{Product}: Getting online release information...", Product);
        try
        {
            // Get all releases
            IReadOnlyList<Release> releasesList = await ReleasesJsonSchema
                .GetReleasesAsync(GetRelease(), cancellationToken)
                .ConfigureAwait(false);
            foreach (Release release in releasesList)
            {
                // We expect only "vms" products
                Debug.Assert(
                    release.Product.Equals(Release.VmsProduct, StringComparison.OrdinalIgnoreCase)
                );

                // Set version
                VersionInfo versionInfo = new();
                Debug.Assert(!string.IsNullOrEmpty(release.Version));
                versionInfo.SetVersion(release.Version);

                // Add the label
                AddLabel(versionInfo, release.GetLabel());

                // Get the build number from the version
                int buildNumber = versionInfo.BuildNumber;

                // Get available packages for this release
                IReadOnlyList<Package> packageList = await PackagesJsonSchema
                    .GetPackagesAsync(GetRelease(), buildNumber, cancellationToken)
                    .ConfigureAwait(false);

                // Get the x64 and arm64 server ubuntu server packages
                Package? packageX64 = packageList.FirstOrDefault(item => item.IsX64Server());
                Debug.Assert(packageX64 != default(Package));
                Debug.Assert(!string.IsNullOrEmpty(packageX64.File));
                Package? packageArm64 = packageList.FirstOrDefault(item => item.IsArm64Server());
                Debug.Assert(packageArm64 != default(Package));
                Debug.Assert(!string.IsNullOrEmpty(packageArm64.File));

                // Create the download URLs
                // https://updates.networkoptix.com/{product}/{build}/{file}
                versionInfo.UriX64 = new Uri(
                    $"https://updates.networkoptix.com/{GetRelease()}/{buildNumber}/{packageX64.File}"
                );
                versionInfo.UriArm64 = new Uri(
                    $"https://updates.networkoptix.com/{GetRelease()}/{buildNumber}/{packageArm64.File}"
                );

                // Verify and add to list
                if (VerifyVersion(versionInfo))
                {
                    _versions.Add(versionInfo);
                }
            }

            // Make sure all labels are correct
            VerifyLabels();
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e))
        {
            // Log and rethrow
            throw;
        }
    }

    private bool VerifyVersion(VersionInfo versionInfo)
    {
        // Static rules:

        // Ubuntu Jammy requires version 5.1 or later
        if (versionInfo.CompareTo("5.1") >= 0)
        {
            return true;
        }

        Log.Logger.Warning(
            "{Product}:{Version} : Ubuntu Jammy requires v5.1+",
            Product,
            versionInfo.Version
        );
        return false;
    }

    public void AddLabel(VersionInfo versionInfo, VersionInfo.LabelType label)
    {
        ArgumentNullException.ThrowIfNull(versionInfo);

        // Ignore if label is None
        if (label == VersionInfo.LabelType.None)
        {
            return;
        }

        // Does this label already exists in other versions
        VersionInfo? existingVersion = _versions.FirstOrDefault(item =>
            item.Labels.Contains(label)
        );
        if (existingVersion == default(VersionInfo))
        {
            // New label
            versionInfo.Labels.Add(label);
            return;
        }

        // Is this version larger than the other version
        if (versionInfo.CompareTo(existingVersion) <= 0)
        {
            return;
        }

        Log.Logger.Warning(
            "{Product}: Replacing {Label} from {ExistingVersion} to {NewVersion}",
            Product,
            label,
            existingVersion.Version,
            versionInfo.Version
        );

        // Remove from other version and add to this version
        _ = existingVersion.Labels.Remove(label);
        versionInfo.Labels.Add(label);
    }

    private VersionInfo? FindMissingLabel(
        VersionInfo.LabelType targetLabel,
        List<VersionInfo.LabelType> sourceLabels
    )
    {
        foreach (VersionInfo.LabelType label in sourceLabels)
        {
            // Find last matching item, must be sorted
            VersionInfo? version = _versions.FindLast(item => item.Labels.Contains(label));
            if (version != default(VersionInfo))
            {
                Log.Logger.Warning(
                    "{Product}: Using {SourceLabel} for {TargetLabel}",
                    Product,
                    label,
                    targetLabel
                );
                return version;
            }
        }
        return default;
    }

    public void VerifyLabels()
    {
        // Sort by version number
        _versions.Sort(new VersionInfoComparer());

        // If no Latest label is set, use Stable or RC or Beta as Latest
        if (!_versions.Any(item => item.Labels.Contains(VersionInfo.LabelType.Latest)))
        {
            VersionInfo latest =
                FindMissingLabel(
                    VersionInfo.LabelType.Latest,
                    [
                        VersionInfo.LabelType.Stable,
                        VersionInfo.LabelType.RC,
                        VersionInfo.LabelType.Beta,
                    ]
                ) ?? throw new InvalidOperationException("Latest label could not be resolved.");
            latest.Labels.Add(VersionInfo.LabelType.Latest);
        }

        // If no Stable label is set, use Latest as stable
        if (!_versions.Any(item => item.Labels.Contains(VersionInfo.LabelType.Stable)))
        {
            VersionInfo stable =
                FindMissingLabel(VersionInfo.LabelType.Stable, [VersionInfo.LabelType.Latest])
                ?? throw new InvalidOperationException("Stable label could not be resolved.");
            stable.Labels.Add(VersionInfo.LabelType.Stable);
        }

        // Remove all versions without labels
        _ = _versions.RemoveAll(item => item.Labels.Count == 0);

        // Sort by label
        foreach (VersionInfo version in _versions)
        {
            version.SortLabels();
        }

        // Must have 1 Latest and 1 Stable label
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            _versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)),
            1
        );
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            _versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)),
            1
        );

        // Must have no more than 1 Beta or RC labels
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            _versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta)),
            1
        );
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            _versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC)),
            1
        );
    }

    public void LogInformation()
    {
        foreach (VersionInfo version in _versions)
        {
            Log.Logger.Information(
                "{Product}: Version: {Version}, Label: {Labels}, UriX64: {UriX64}, UriArm64: {UriArm64}",
                Product,
                version.Version,
                version.Labels,
                version.UriX64,
                version.UriArm64
            );
        }
    }

    /// <summary>
    /// Verifies all download URLs for the loaded versions.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the request.
    /// </param>
    public async Task VerifyUrlsAsync(CancellationToken cancellationToken)
    {
        try
        {
            HttpClient httpClient = HttpClientFactory.GetHttpClient();
            foreach (VersionInfo versionUri in _versions)
            {
                // Will throw on error
                await VerifyUrlAsync(httpClient, versionUri.UriX64, cancellationToken)
                    .ConfigureAwait(false);
                await VerifyUrlAsync(httpClient, versionUri.UriArm64, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception e) when (Log.Logger.LogAndHandle(e))
        {
            // Log and rethrow
            throw;
        }
    }

    private static async Task VerifyUrlAsync(
        HttpClient httpClient,
        Uri url,
        CancellationToken cancellationToken
    )
    {
        // Will throw on failure
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(url);

        // Get URL
        Log.Logger.Information("Verifying Url: {Url}", url);
        using HttpResponseMessage httpResponse = await httpClient
            .GetAsync(url, cancellationToken)
            .ConfigureAwait(false);
        _ = httpResponse.EnsureSuccessStatusCode();

        // Get filename from httpResponse or Uri path
        string? fileName = httpResponse.Content.Headers.ContentDisposition?.FileName;
        fileName ??= Path.GetFileName(url.LocalPath);

        // Log details
        Log.Logger.Information(
            "File Name: {FileName}, File Size: {FileSize}, Last Modified: {LastModified}",
            fileName,
            httpResponse.Content.Headers.ContentLength,
            httpResponse.Content.Headers.LastModified
        );
    }

    public void Verify()
    {
        // Match verification logic executed during GetVersions()

        // Verify each version
        List<VersionInfo> removeVersions = [];
        foreach (VersionInfo? version in _versions.Where(version => !VerifyVersion(version)))
        {
            Log.Logger.Warning("{Product} : Removing {Version}", Product, version.Version);
            removeVersions.Add(version);
        }
        _ = _versions.RemoveAll(removeVersions.Contains);

        // Verify labels
        VerifyLabels();
    }
}
