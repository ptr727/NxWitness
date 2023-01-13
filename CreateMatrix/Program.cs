using System.CommandLine;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;

namespace CreateMatrix;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Configure logger
        ConfigureLogger();

        // Create command line options
        var rootCommand = CreateCommandLine();

        // Run
        return rootCommand.Invoke(args);
    }

    private static RootCommand CreateCommandLine()
    {
        var versionOption = new Option<string>(
            name: "--version",
            description: "Version JSON file.",
            getDefaultValue: () => "Version.json");

        var matrixOption = new Option<string>(
            name: "--matrix",
            description: "Matrix JSON file.",
            getDefaultValue: () => "Matrix.json");

        var schemaVersionOption = new Option<string>(
            name: "--schemaversion",
            description: "Version JSON schema file.",
            getDefaultValue: () => "Version.schema.json");

        var schemaMatrixOption = new Option<string>(
            name: "--schemamatrix",
            description: "Matrix JSON schema file.",
            getDefaultValue: () => "Matrix.schema.json");

        var onlineOption = new Option<bool>(
            name: "--online",
            description: "Update versions using online release information",
            getDefaultValue: () => false);

        var defaultCommand = new Command("defaults", "Write defaults to version file")
        {
            versionOption,
            onlineOption
        };
        defaultCommand.SetHandler((version, online) => { DefaultHandler(version, online); },
            versionOption, onlineOption);

        var matrixCommand = new Command("matrix", "Create matrix file from version information")
        {
            versionOption,
            matrixOption,
            onlineOption
        };
        matrixCommand.SetHandler((version, matrix, online) => { MatrixHandler(version, matrix, online); },
            versionOption, matrixOption, onlineOption);

        var schemaCommand = new Command("schema", "Write version and matrix schema files")
        {
            schemaVersionOption,
            schemaMatrixOption
        };
        schemaCommand.SetHandler((version, matrix) => { SchemaHandler(version, matrix); },
            schemaVersionOption, schemaMatrixOption);

        var rootCommand =
            new RootCommand("CreateMatrix utility to create a matrix of builds from a list of product versions")
            {
                defaultCommand,
                matrixCommand,
                schemaCommand
            };
        return rootCommand;
    }

    private static void ConfigureLogger()
    {
        // Configure serilog console logging
        SelfLog.Enable(Console.Error);
        LoggerConfiguration loggerConfiguration = new();
        loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}");
        Log.Logger = loggerConfiguration.CreateLogger();
    }

    private static Task<int> SchemaHandler(string schemaVersionPath, string schemaMatrixPath)
    {
        Log.Logger.Information("Writing schema to file : Version Path: {VersionPath}, Matrix Path: {MatrixPath}", schemaVersionPath, schemaMatrixPath);
        VersionJsonSchema.GenerateSchema(schemaVersionPath);
        MatrixJsonSchema.GenerateSchema(schemaMatrixPath);
        return Task.FromResult(0);
    }

    private static Task<int> DefaultHandler(string versionPath, bool onlineUpdate)
    {
        Log.Logger.Information("Writing defaults to file : Version Path: {VersionPath}, Online Updates: {OnlineUpdate}",
            versionPath, onlineUpdate);

        // Use static defaults or online versions
        VersionJsonSchema versionSchema = new();
        if (!onlineUpdate)
        {
            Log.Logger.Information("Using static version defaults");

            // Use static information
            versionSchema.Products = ProductInfo.GetDefaults();
        }
        else
        {
            Log.Logger.Information("Getting online version information...");

            // Get versions for all products using releases API
            versionSchema.Products = ProductInfo.GetProducts();
            versionSchema.Products.ForEach(item => item.GetReleasesVersions());
        }

        // Log info
        versionSchema.Products.ForEach(item => item.LogInformation());

        // Verify Urls
        versionSchema.Products.ForEach(item => item.VerifyUrls());

        // Write to file
        Log.Logger.Information("Writing versions to {Path}", versionPath);
        VersionJsonSchema.ToFile(versionPath, versionSchema);

        return Task.FromResult(0);
    }

    private static Task<int> MatrixHandler(string versionPath, string matrixPath, bool onlineUpdate)
    {
        Log.Logger.Information(
            "Creating image matrix from versions : Version Path: {VersionPath}, Matrix Path: {MatrixPath}, Online Updates: {OnlineUpdate}",
            versionPath, matrixPath, onlineUpdate);

        // Load versions from file or online
        VersionJsonSchema versionSchema = new();
        if (!onlineUpdate)
        {
            Log.Logger.Information("Reading versions from {Path}", versionPath);
            versionSchema = VersionJsonSchema.FromFile(versionPath);
        }
        else
        {
            Log.Logger.Information("Getting online version information...");
            versionSchema.Products = ProductInfo.GetProducts();
            versionSchema.Products.ForEach(item => item.GetReleasesVersions());
        }

        // Remove all stable labels
        // https://github.com/ptr727/NxWitness/issues/62
        versionSchema.Products.ForEach(product =>
            product.Versions.ForEach(version =>
                version.Labels.RemoveAll(label => string.Equals(label, VersionUri.StableLabel))));

        // Log info
        versionSchema.Products.ForEach(item => item.LogInformation());

        // Verify Urls
        versionSchema.Products.ForEach(item => item.VerifyUrls());

        // Create matrix
        Log.Logger.Information("Creating Matrix from versions");
        MatrixJsonSchema matrixSchema = new()
        {
            Images = ImageInfo.CreateImages(versionSchema.Products)
        };
        Log.Logger.Information("Created {Count} images in matrix", matrixSchema.Images.Count);

        // Disable GHCR posting
        // https://github.com/ptr727/NxWitness/issues/69
        matrixSchema.Images.ForEach(image =>
            image.Tags.RemoveAll(tag => tag.Contains("ghcr.io")));

       // Log info
        matrixSchema.Images.ForEach(item => item.LogInformation());

        // Write matrix
        Log.Logger.Information("Writing matrix to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixSchema);

        return Task.FromResult(0);
    }
}