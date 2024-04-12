using System.Text;
using Serilog;

namespace CreateMatrix;

public class ComposeFile
{
    public static void Create(string makePath)
    {
        // Create local Compose file
        var composeFile = CreateComposefile(null);
        var filePath = Path.Combine(makePath, "Test.yml");
        Log.Logger.Information("Writing Compose file to {Path}", filePath);
        File.WriteAllText(filePath, composeFile, Encoding.UTF8);

        // Create develop Compose file
        composeFile = CreateComposefile("develop");
        filePath = Path.Combine(makePath, "Test-develop.yml");
        Log.Logger.Information("Writing Compose file to {Path}", filePath);
        File.WriteAllText(filePath, composeFile, Encoding.UTF8);

        // Create latest Compose file
        composeFile = CreateComposefile("latest");
        filePath = Path.Combine(makePath, "Test-latest.yml");
        Log.Logger.Information("Writing Compose file to {Path}", filePath);
        File.WriteAllText(filePath, composeFile, Encoding.UTF8);
    }

    private static string CreateComposefile(string? label)
    {
        // TODO: Switch to volume sub-paths on Moby v26+
        // https://github.com/moby/moby/pull/45687

        // Compose file header
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("""
            # Compose file created by CreateMatrix, do not modify by hand

            """);
        stringBuilder.AppendLine();

        // Create volumes
        stringBuilder.Append(CreateVolumes());
        stringBuilder.AppendLine();

        // Create services
        stringBuilder.Append(CreateServices(label));
        stringBuilder.AppendLine();

        return stringBuilder.ToString().Replace("\r\n", "\n").Trim();
    }

    private static string CreateVolumes()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("""
            volumes:

            """);
        stringBuilder.AppendLine();

        // Create a volume for every product
        foreach (var productType in ProductInfo.GetProductTypes())
        {
            // Standard
            stringBuilder.Append(CreateVolume(productType, false));
            stringBuilder.AppendLine();

            // LSIO
            stringBuilder.Append(CreateVolume(productType, true));
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    private static string CreateVolume(ProductInfo.ProductType productType, bool lsio)
    {
        if (lsio) 
            return $$"""
              # Dockerfile : {{ProductInfo.GetDocker(productType, lsio)}}
              test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_config:
              test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_media:

            """;
        else
            return $$"""
              # Dockerfile : {{ProductInfo.GetDocker(productType, lsio)}}
              test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_etc:
              test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_ini:
              test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_var:
              test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_media:

            
            """
            ;
    }

    private static string CreateServices(string? label)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("""
            services:

            """);
        stringBuilder.AppendLine();

        // Create a service for every product
        int standardPort = 7101, lsioPort = 7201;
        foreach (var productType in ProductInfo.GetProductTypes())
        {
            // Standard
            stringBuilder.Append(CreateService(productType, false, standardPort ++, label));
            stringBuilder.AppendLine();

            // LSIO
            stringBuilder.Append(CreateService(productType, true, lsioPort ++, label));
            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    private static string CreateService(ProductInfo.ProductType productType, bool lsio, int port, string? label)
    {
        var image = string.IsNullOrEmpty(label) ? $"test_{ProductInfo.GetDocker(productType, lsio).ToLower()}" : $"docker.io/ptr727/{ProductInfo.GetDocker(productType, lsio).ToLower()}:{label}";
        var service = $$"""
              # Dockerfile : {{ProductInfo.GetDocker(productType, lsio)}}
              # Port : {{port}}
              {{ProductInfo.GetDocker(productType, lsio).ToLower()}}:
                image: {{image}}
                container_name: {{ProductInfo.GetDocker(productType, lsio).ToLower()}}-container
                restart: unless-stopped
                environment:
                  - TZ=Americas/Los_Angeles
                network_mode: bridge
                ports:
                  - {{port}}:7001

            """;

        if (lsio)
            service += $$"""
                volumes:
                  - test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_config:/config
                  - test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_media:/media

            """;
        else
            service += $$"""
                volumes:
                  - test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_etc:/opt/{{ProductInfo.GetCompany(productType).ToLower()}}/mediaserver/etc
                  - test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_ini:/home/{{ProductInfo.GetCompany(productType).ToLower()}}/.config/nx_ini
                  - test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_var:/opt/{{ProductInfo.GetCompany(productType).ToLower()}}/mediaserver/var
                  - test_{{ProductInfo.GetDocker(productType, lsio).ToLower()}}_media:/media

            """;

        return service;
    }
}
