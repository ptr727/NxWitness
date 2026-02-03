namespace CreateMatrix;

internal static class ComposeFile
{
    public static void Create(DirectoryInfo makeDirectory)
    {
        string makePath = makeDirectory.FullName;

        // Create local Compose file
        string composeFile = CreateComposefile(null);
        string filePath = Path.Combine(makePath, "Test.yml");
        Log.Logger.Information("Writing Compose file to {Path}", filePath);
        File.WriteAllText(filePath, composeFile);

        // Create develop Compose file
        composeFile = CreateComposefile("develop");
        filePath = Path.Combine(makePath, "Test-develop.yml");
        Log.Logger.Information("Writing Compose file to {Path}", filePath);
        File.WriteAllText(filePath, composeFile);

        // Create latest Compose file
        composeFile = CreateComposefile("latest");
        filePath = Path.Combine(makePath, "Test-latest.yml");
        Log.Logger.Information("Writing Compose file to {Path}", filePath);
        File.WriteAllText(filePath, composeFile);
    }

    private static string CreateComposefile(string? label)
    {
        // TODO: Switch to volume sub-paths on Moby v26+
        // https://github.com/moby/moby/pull/45687

        // Compose file header
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(
            """
            # Compose file created by CreateMatrix, do not modify by hand

            """
        );
        _ = stringBuilder.AppendLineCrlf();

        // Create volumes
        _ = stringBuilder.Append(CreateVolumes());
        _ = stringBuilder.AppendLineCrlf();
        // Create services
        _ = stringBuilder.Append(CreateServices(label));
        _ = stringBuilder.AppendLineCrlf();

        return stringBuilder.ToString();
    }

    private static string CreateVolumes()
    {
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(
            """
            volumes:

            """
        );
        _ = stringBuilder.AppendLineCrlf();

        // Create a volume for every product
        foreach (ProductInfo.ProductType productType in ProductInfo.GetProductTypes())
        {
            // Standard
            _ = stringBuilder.Append(CreateVolume(productType, false));
            _ = stringBuilder.AppendLineCrlf();

            // LSIO
            _ = stringBuilder.Append(CreateVolume(productType, true));
            _ = stringBuilder.AppendLineCrlf();
        }

        return stringBuilder.ToString();
    }

    private static string CreateVolume(ProductInfo.ProductType productType, bool lsio) =>
        lsio
            ? $$"""
                  # Dockerfile : {{ProductInfo.GetDocker(productType, lsio)}}
                  test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_config:
                  test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_media:

                """
            : $$"""
                  # Dockerfile : {{ProductInfo.GetDocker(productType, lsio)}}
                  test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_etc:
                  test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_ini:
                  test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_var:
                  test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_media:


                """;

    private static string CreateServices(string? label)
    {
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(
            """
            services:

            """
        );
        _ = stringBuilder.AppendLineCrlf();

        // Create a service for every product
        int standardPort = 7101,
            lsioPort = 7201;
        foreach (ProductInfo.ProductType productType in ProductInfo.GetProductTypes())
        {
            // Standard
            _ = stringBuilder.Append(CreateService(productType, false, standardPort++, label));
            _ = stringBuilder.AppendLineCrlf();
            // LSIO
            _ = stringBuilder.Append(CreateService(productType, true, lsioPort++, label));
            _ = stringBuilder.AppendLineCrlf();
        }

        return stringBuilder.ToString();
    }

    private static string CreateService(
        ProductInfo.ProductType productType,
        bool lsio,
        int port,
        string? label
    )
    {
        string image = string.IsNullOrEmpty(label)
            ? $"test_{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}"
            : $"docker.io/ptr727/{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}:{label}";
        string service = $$"""
              # Dockerfile : {{ProductInfo.GetDocker(productType, lsio)}}
              # Port : {{port}}
              {{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}:
                image: {{image}}
                container_name: {{ProductInfo.GetDocker(
                productType,
                lsio
            ).ToLowerInvariant()}}-container
                restart: unless-stopped
                environment:
                  - TZ=Americas/Los_Angeles
                network_mode: bridge
                ports:
                  - {{port}}:7001

            """;

        if (lsio)
        {
            service += $$"""
                    volumes:
                      - test_{{ProductInfo.GetDocker(
                    productType,
                    lsio
                ).ToLowerInvariant()}}_config:/config
                      - test_{{ProductInfo.GetDocker(
                    productType,
                    lsio
                ).ToLowerInvariant()}}_media:/media

                """;
        }
        else
        {
            service += $$"""
                    volumes:
                      - test_{{ProductInfo.GetDocker(
                    productType,
                    lsio
                ).ToLowerInvariant()}}_etc:/opt/{{ProductInfo.GetCompany(
                    productType
                ).ToLowerInvariant()}}/mediaserver/etc
                      - test_{{ProductInfo.GetDocker(
                    productType,
                    lsio
                ).ToLowerInvariant()}}_ini:/home/{{ProductInfo.GetCompany(
                    productType
                ).ToLowerInvariant()}}/.config/nx_ini
                      - test_{{ProductInfo.GetDocker(
                    productType,
                    lsio
                ).ToLowerInvariant()}}_var:/opt/{{ProductInfo.GetCompany(
                    productType
                ).ToLowerInvariant()}}/mediaserver/var
                      - test_{{ProductInfo.GetDocker(productType, lsio).ToLowerInvariant()}}_media:/media

                """;
        }

        return service;
    }
}
