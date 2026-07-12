namespace CreateMatrix;

internal sealed class ProductInfo
{
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

    public List<VersionInfo> Versions { get; set; } = [];

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
            ProductType.None => throw new InvalidOperationException(
                $"{nameof(ProductType)} is None"
            ),
            _ => throw new InvalidEnumArgumentException(
                nameof(productType),
                (int)productType,
                typeof(ProductType)
            ),
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
            ProductType.None => throw new InvalidOperationException(
                $"{nameof(ProductType)} is None"
            ),
            _ => throw new InvalidEnumArgumentException(
                nameof(productType),
                (int)productType,
                typeof(ProductType)
            ),
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
            ProductType.None => throw new InvalidOperationException(
                $"{nameof(ProductType)} is None"
            ),
            _ => throw new InvalidEnumArgumentException(
                nameof(productType),
                (int)productType,
                typeof(ProductType)
            ),
        };

    // Dockerfile name, excluding the .Dockerfile extension.
    // This is the single source of the image/Dockerfile naming convention. ImageInfo derives its
    // name from here. The base variant is a bool (Ubuntu vs LSIO). Promote it to an enum only if a
    // third base type is ever added (which would also ripple through ComposeFile/DockerFile).
    public static string GetDocker(ProductType productType, bool lsio) =>
        $"{productType}{(lsio ? "-LSIO" : "")}";

    public static IEnumerable<ProductType> GetProductTypes() =>
        // Create list of product types
        [.. Enum.GetValues<ProductType>().Where(productType => productType != ProductType.None)];

    public static List<ProductInfo> GetProducts() =>
        // Create list of all known products
        [
            .. from ProductType productType in GetProductTypes()
            select new ProductInfo { Product = productType },
        ];

    public async Task FetchVersionsAsync(CancellationToken cancellationToken)
    {
        // Get version information using releases.json and package.json
        Log.Logger.Information("{Product}: Getting online release information...", Product);
        try
        {
            // Get all releases
            List<Release> releasesList = await ReleasesJsonSchema
                .GetReleasesAsync(GetRelease(), cancellationToken)
                .ConfigureAwait(false);
            foreach (Release release in releasesList)
            {
                // Only process "vms" products
                if (!release.Product.Equals(Release.VmsProduct, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Logger.Warning(
                        "{Product}: Skipping {ReleaseProduct}",
                        Product,
                        release.Product
                    );
                    continue;
                }

                // Set the version and label
                VersionInfo versionInfo = CreateVersionInfo(release);

                // Get the build number from the version
                int buildNumber = versionInfo.GetBuildNumber();

                // Get available packages for this release
                List<Package> packageList = await PackagesJsonSchema
                    .GetPackagesAsync(GetRelease(), buildNumber, cancellationToken)
                    .ConfigureAwait(false);

                // Get the x64 and arm64 server ubuntu server packages
                Package packageX64 =
                    packageList.Find(item => item.IsX64Server())
                    ?? throw new InvalidOperationException(
                        $"{Product}: No x64 Ubuntu server package found for build {buildNumber} (version {versionInfo.Version})"
                    );
                if (string.IsNullOrEmpty(packageX64.File))
                {
                    throw new InvalidOperationException(
                        $"{Product}: x64 Ubuntu server package for build {buildNumber} (version {versionInfo.Version}) has no file name"
                    );
                }
                Package packageArm64 =
                    packageList.Find(item => item.IsArm64Server())
                    ?? throw new InvalidOperationException(
                        $"{Product}: No arm64 Ubuntu server package found for build {buildNumber} (version {versionInfo.Version})"
                    );
                if (string.IsNullOrEmpty(packageArm64.File))
                {
                    throw new InvalidOperationException(
                        $"{Product}: arm64 Ubuntu server package for build {buildNumber} (version {versionInfo.Version}) has no file name"
                    );
                }

                // Create the download URLs
                // https://updates.networkoptix.com/{product}/{build}/{file}
                versionInfo.UriX64 =
                    $"https://updates.networkoptix.com/{GetRelease()}/{buildNumber}/{packageX64.File}";
                versionInfo.UriArm64 =
                    $"https://updates.networkoptix.com/{GetRelease()}/{buildNumber}/{packageArm64.File}";

                // Verify and add to list
                if (VerifyVersion(versionInfo))
                {
                    Versions.Add(versionInfo);
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

    public VersionInfo CreateVersionInfo(Release release)
    {
        // Create a version with its label from the release.
        // Note: AddLabel() may move the label off other versions already in the list.
        if (string.IsNullOrEmpty(release.Version))
        {
            throw new InvalidOperationException(
                $"{Product}: Release has no version (publication type '{release.PublicationType}')"
            );
        }
        VersionInfo versionInfo = new();
        versionInfo.SetVersion(release.Version);
        AddLabel(versionInfo, release.GetLabel());
        return versionInfo;
    }

    private bool VerifyVersion(VersionInfo versionInfo)
    {
        // Static rules:

        // Ubuntu Noble requires version 6.0 or later
        if (versionInfo.CompareTo("6.0") >= 0)
        {
            return true;
        }

        Log.Logger.Warning(
            "{Product}:{Version} : Ubuntu Noble requires v6.0+",
            Product,
            versionInfo.Version
        );
        return false;
    }

    public void AddLabel(VersionInfo versionInfo, VersionInfo.LabelType label)
    {
        // Ignore if label is None
        if (label == VersionInfo.LabelType.None)
        {
            return;
        }

        // Does this label already exists in other versions
        VersionInfo? existingVersion = Versions.Find(item => item.Labels.Contains(label));
        if (existingVersion == null)
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
            VersionInfo? version = Versions.FindLast(item => item.Labels.Contains(label));
            if (version == null)
            {
                continue;
            }

            Log.Logger.Warning(
                "{Product}: Using {SourceLabel} for {TargetLabel}",
                Product,
                label,
                targetLabel
            );
            return version;
        }
        return null;
    }

    public void VerifyLabels()
    {
        // Sort by version number
        Versions.Sort(new VersionInfoComparer());

        // If no Latest label is set, use Stable or RC or Beta as Latest
        if (!Versions.Any(item => item.Labels.Contains(VersionInfo.LabelType.Latest)))
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
        if (!Versions.Any(item => item.Labels.Contains(VersionInfo.LabelType.Stable)))
        {
            VersionInfo stable =
                FindMissingLabel(VersionInfo.LabelType.Stable, [VersionInfo.LabelType.Latest])
                ?? throw new InvalidOperationException("Stable label could not be resolved.");
            stable.Labels.Add(VersionInfo.LabelType.Stable);
        }

        // Remove all versions without labels
        _ = Versions.RemoveAll(item => item.Labels.Count == 0);
        // Sort by label
        Versions.ForEach(item => item.Labels.Sort());

        // Must have 1 Latest and 1 Stable label
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Latest)),
            1
        );
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Stable)),
            1
        );

        // Must have no more than 1 Beta or RC labels
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.Beta)),
            1
        );
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            Versions.Count(item => item.Labels.Contains(VersionInfo.LabelType.RC)),
            1
        );

        // Must have no duplicate version numbers
        VerifyNoDuplicateVersions();
    }

    public void VerifyNoDuplicateVersions()
    {
        // Each version number must appear at most once.
        // Duplicates collapse when the matrix builds a set keyed by version number
        // (see ImageInfo.CreateImages() and VersionInfoComparer) and must be rejected
        // rather than written to the version file.
        List<string> duplicateVersions =
        [
            .. Versions
                .GroupBy(item => VersionInfo.ParseVersion(item.Version))
                .Where(group => group.Count() > 1)
                .Select(group => group.First().Version),
        ];
        if (duplicateVersions.Count > 0)
        {
            throw new InvalidOperationException(
                $"{Product}: Duplicate version numbers found: {string.Join(", ", duplicateVersions)}"
            );
        }
    }

    public void LogInformation()
    {
        foreach (VersionInfo version in Versions)
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

    public async Task VerifyUrlsAsync(CancellationToken cancellationToken)
    {
        try
        {
            HttpClient httpClient = HttpClientFactory.GetHttpClient();
            foreach (VersionInfo versionUri in Versions)
            {
                // Will throw on error
                await VerifyUrlAsync(httpClient, new Uri(versionUri.UriX64), cancellationToken)
                    .ConfigureAwait(false);
                await VerifyUrlAsync(httpClient, new Uri(versionUri.UriArm64), cancellationToken)
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
        foreach (VersionInfo version in Versions.Where(version => !VerifyVersion(version)))
        {
            Log.Logger.Warning("{Product} : Removing {Version}", Product, version.Version);
            removeVersions.Add(version);
        }
        _ = Versions.RemoveAll(removeVersions.Contains);

        // Verify labels
        VerifyLabels();
    }
}
