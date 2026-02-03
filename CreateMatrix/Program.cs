using Serilog.Sinks.SystemConsole.Themes;

// dotnet publish --self-contained false --output ./publish
// ./publish/CreateMatrix matrix --updateversion --matrixpath ./JSON/Matrix.json --versionpath ./JSON/Version.json
// echo $?

namespace CreateMatrix;

internal sealed class Program(
    CommandLine.Options commandLineOptions,
    CancellationToken cancellationToken
)
{
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse commandline
            CommandLine commandLine = new(args);

            // Bypass startup for errors or help and version commands
            if (CommandLine.BypassStartup(commandLine.Result))
            {
                return await commandLine.Result.InvokeAsync().ConfigureAwait(false);
            }

            // Log to the console
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    formatProvider: CultureInfo.InvariantCulture,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [t:{ThreadId}{ThreadName}] {Message:lj}{NewLine}{Exception}"
                );
            Log.Logger = loggerConfiguration.CreateLogger();

            // Invoke command
            return await commandLine.Result.InvokeAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Log.Logger.Warning("Operation was cancelled.");
            return 130; // POSIX standard for SIGINT
        }
        catch (Exception ex) when (Log.Logger.LogAndHandle(ex))
        {
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    internal async Task<int> ExecuteSchemaAsync()
    {
        Log.Logger.Information(
            "Writing schema to file : Version Path: {VersionPath}, Matrix Path: {MatrixPath}",
            commandLineOptions.VersionSchemaPath,
            commandLineOptions.MatrixSchemaPath
        );
        VersionJsonSchema.GenerateSchema(commandLineOptions.VersionSchemaPath);
        MatrixJsonSchema.GenerateSchema(commandLineOptions.MatrixSchemaPath);

        return 0;
    }

    internal async Task<int> ExecuteVersionAsync()
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
            await productInfo.FetchVersionsAsync(cancellationToken).ConfigureAwait(false);
            productInfo.LogInformation();
            await productInfo.VerifyUrlsAsync(cancellationToken).ConfigureAwait(false);
        }

        // Write to file
        Log.Logger.Information(
            "Writing version information to {Path}",
            commandLineOptions.VersionPath
        );
        VersionJsonSchema.ToFile(commandLineOptions.VersionPath, versionSchema);

        return 0;
    }

    internal async Task<int> ExecuteMatrixAsync()
    {
        // Load version info from file
        Log.Logger.Information(
            "Reading version information from {Path}",
            commandLineOptions.VersionPath
        );
        VersionJsonSchema fileSchema = VersionJsonSchema.FromFile(commandLineOptions.VersionPath);
        // Re-verify as rules may have changed after file was written
        foreach (ProductInfo productInfo in fileSchema.Products)
        {
            productInfo.Verify();
            productInfo.LogInformation();
        }

        // Update version information
        if (commandLineOptions.UpdateVersion)
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
                await productInfo.FetchVersionsAsync(cancellationToken).ConfigureAwait(false);
                productInfo.LogInformation();
            }

            // Make sure the labelled version numbers do not regress
            List<ProductInfo> fileProducts = [.. fileSchema.Products];
            List<ProductInfo> onlineProducts = [.. onlineSchema.Products];
            ReleaseVersionForward.Verify(fileProducts, onlineProducts);

            // Verify URL's
            foreach (ProductInfo productInfo in onlineSchema.Products)
            {
                await productInfo.VerifyUrlsAsync(cancellationToken).ConfigureAwait(false);
            }

            // Update the file version with the online version
            Log.Logger.Information(
                "Writing version information to {Path}",
                commandLineOptions.VersionPath
            );
            VersionJsonSchema.ToFile(commandLineOptions.VersionPath, onlineSchema);
            fileSchema = onlineSchema;
        }
        else
        {
            // Verify URL's
            foreach (ProductInfo productInfo in fileSchema.Products)
            {
                await productInfo.VerifyUrlsAsync(cancellationToken).ConfigureAwait(false);
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
        Log.Logger.Information(
            "Writing matrix information to {Path}",
            commandLineOptions.MatrixPath
        );
        MatrixJsonSchema.ToFile(commandLineOptions.MatrixPath, matrixSchema);

        return 0;
    }

    internal Task<int> ExecuteMakeAsync()
    {
        // Load version info from file
        Log.Logger.Information(
            "Reading version information from {Path}",
            commandLineOptions.VersionPath
        );
        VersionJsonSchema versionSchema = VersionJsonSchema.FromFile(
            commandLineOptions.VersionPath
        );

        // Create Compose files
        ComposeFile.Create(commandLineOptions.MakeDirectory);

        // Create Docker files
        List<ProductInfo> products = [.. versionSchema.Products];
        DockerFile.Create(
            products,
            commandLineOptions.DockerDirectory,
            commandLineOptions.VersionLabel
        );

        return Task.FromResult(0);
    }
}
