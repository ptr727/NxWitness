using System.CommandLine;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;

// dotnet publish --self-contained false --output ./publish
// ./publish/CreateMatrix matrix --update --matrix ./JSON/Matrix.json --version ./JSON/Version.json
// echo $?

namespace CreateMatrix;

public sealed class Program(CancellationToken cancellationToken)
{
    private readonly CancellationToken _cancellationToken = cancellationToken;

    private static async Task<int> Main(string[] args)
    {
        // Configure logger
        ConfigureLogger();

        // Create command line options
        RootCommand rootCommand = CommandLine.CreateRootCommand();

        // Run
        // 0 == ok, 1 == error
        ParserConfiguration parserConfiguration = new();
        ParseResult parseResult = rootCommand.Parse(args, parserConfiguration);
        InvocationConfiguration invocationConfiguration = new()
        {
            ProcessTerminationTimeout = TimeSpan.FromSeconds(2),
        };
        return await parseResult
            .InvokeAsync(invocationConfiguration, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static void ConfigureLogger()
    {
        // Configure serilog console logging
        SelfLog.Enable(Console.Error);
        LoggerConfiguration loggerConfiguration = new();
        _ = loggerConfiguration.WriteTo.Console(
            formatProvider: CultureInfo.InvariantCulture,
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}"
        );
        Log.Logger = loggerConfiguration.CreateLogger();
    }

    internal Task<int> SchemaHandler(string schemaVersionPath, string schemaMatrixPath)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        Log.Logger.Information(
            "Writing schema to file : Version Path: {VersionPath}, Matrix Path: {MatrixPath}",
            schemaVersionPath,
            schemaMatrixPath
        );
        VersionJsonSchema.GenerateSchema(schemaVersionPath);
        MatrixJsonSchema.GenerateSchema(schemaMatrixPath);
        return Task.FromResult(0);
    }

    internal async Task<int> VersionHandler(string versionPath)
    {
        // Get versions for all products using releases API
        VersionJsonSchema versionSchema = new();
        Log.Logger.Information("Getting version information online...");
        foreach (ProductInfo productInfo in ProductInfo.GetProducts())
        {
            versionSchema.Products.Add(productInfo);
        }
        foreach (ProductInfo productInfo in versionSchema.Products)
        {
            await productInfo.FetchVersionsAsync(_cancellationToken).ConfigureAwait(false);
            productInfo.LogInformation();
            await productInfo.VerifyUrlsAsync(_cancellationToken).ConfigureAwait(false);
        }

        // Write to file
        Log.Logger.Information("Writing version information to {Path}", versionPath);
        VersionJsonSchema.ToFile(versionPath, versionSchema);

        return 0;
    }

    internal async Task<int> MatrixHandler(
        string versionPath,
        string matrixPath,
        bool updateVersion
    )
    {
        // Load version info from file
        Log.Logger.Information("Reading version information from {Path}", versionPath);
        VersionJsonSchema fileSchema = VersionJsonSchema.FromFile(versionPath);

        // Re-verify as rules may have changed after file was written
        foreach (ProductInfo productInfo in fileSchema.Products)
        {
            productInfo.Verify();
            productInfo.LogInformation();
        }

        // Update version information
        if (updateVersion)
        {
            // Get versions for all products using releases API
            VersionJsonSchema onlineSchema = new();
            Log.Logger.Information("Getting version information online...");
            foreach (ProductInfo productInfo in ProductInfo.GetProducts())
            {
                onlineSchema.Products.Add(productInfo);
            }
            foreach (ProductInfo productInfo in onlineSchema.Products)
            {
                await productInfo.FetchVersionsAsync(_cancellationToken).ConfigureAwait(false);
                productInfo.LogInformation();
            }

            // Make sure the labelled version numbers do not regress
            List<ProductInfo> fileProducts = [.. fileSchema.Products];
            List<ProductInfo> onlineProducts = [.. onlineSchema.Products];
            ReleaseVersionForward.Verify(fileProducts, onlineProducts);

            // Verify URL's
            foreach (ProductInfo productInfo in onlineSchema.Products)
            {
                await productInfo.VerifyUrlsAsync(_cancellationToken).ConfigureAwait(false);
            }

            // Update the file version with the online version
            Log.Logger.Information("Writing version information to {Path}", versionPath);
            VersionJsonSchema.ToFile(versionPath, onlineSchema);
            fileSchema = onlineSchema;
        }
        else
        {
            // Verify URL's
            foreach (ProductInfo productInfo in fileSchema.Products)
            {
                await productInfo.VerifyUrlsAsync(_cancellationToken).ConfigureAwait(false);
            }
        }

        // Create matrix
        Log.Logger.Information("Creating Matrix from versions");
        MatrixJsonSchema matrixSchema = new();
        List<ProductInfo> products = [.. fileSchema.Products];
        IReadOnlyList<ImageInfo> images = ImageInfo.CreateImages(products);
        foreach (ImageInfo imageInfo in images)
        {
            matrixSchema.Images.Add(imageInfo);
        }
        Log.Logger.Information("Created {Count} images in matrix", matrixSchema.Images.Count);

        // Log info
        foreach (ImageInfo imageInfo in matrixSchema.Images)
        {
            imageInfo.LogInformation();
        }

        // Write matrix
        Log.Logger.Information("Writing matrix information to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixSchema);

        return 0;
    }

    internal Task<int> MakeHandler(
        string versionPath,
        string makePath,
        string dockerPath,
        VersionInfo.LabelType label
    )
    {
        _cancellationToken.ThrowIfCancellationRequested();
        // Load version info from file
        Log.Logger.Information("Reading version information from {Path}", versionPath);
        VersionJsonSchema versionSchema = VersionJsonSchema.FromFile(versionPath);

        // Create Compose files
        ComposeFile.Create(makePath);

        // Create Docker files
        List<ProductInfo> products = [.. versionSchema.Products];
        DockerFile.Create(products, dockerPath, label);

        return Task.FromResult(0);
    }
}
