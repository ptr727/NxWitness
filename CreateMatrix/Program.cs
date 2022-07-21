using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;
using System.CommandLine;

namespace CreateMatrix;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Configure serilog console logging
        SelfLog.Enable(Console.Error);
        LoggerConfiguration loggerConfiguration = new();
        loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}");
        Log.Logger = loggerConfiguration.CreateLogger();

        // Configure CLI options
        var versionOption = new Option<string>(
            name: "--version",
            description: "Version JSON file.",
            getDefaultValue: () => "Version.json");

        var matrixOption = new Option<string>(
            name: "--matrix",
            description: "Matrix JSON file.",
            getDefaultValue: () => "Matrix.json");

        var schemaOption = new Option<string>(
            name: "--schema",
            description: "Version JSON schema file.",
            getDefaultValue: () => "Version.schema.json");

        var onlineOption = new Option<bool>(
            name: "--online",
            description: "Update versions from online release information.",
            getDefaultValue: () => false);

        var defaultCommand = new Command("defaults", "Write defaults to version file.")
        {
            versionOption,
            onlineOption
        };
        defaultCommand.SetHandler((version, online) => { DefaultHandler(version, online); },
            versionOption, onlineOption);

        var matrixCommand = new Command("matrix", "Create matrix file from version information.")
        {
            versionOption,
            matrixOption,
            onlineOption
        };
        matrixCommand.SetHandler((version, matrix, online) => { MatrixHandler(version, matrix, online); },
            versionOption, matrixOption, onlineOption);

        var schemaCommand = new Command("schema", "Write version schema file.")
        {
            schemaOption
        };
        schemaCommand.SetHandler(schema => { SchemaHandler(schema); },
            schemaOption);

        var rootCommand =
            new RootCommand("CreateMatrix utility to create a matrix of builds from a list of product versions.")
            {
                defaultCommand,
                matrixCommand,
                schemaCommand
            };

        // Run
        return rootCommand.Invoke(args);
    }

    private static Task<int> SchemaHandler(string schemaPath)
    {
        Log.Logger.Information("Writing schema to file : Schema Path: {SchemaPath}", schemaPath);

        // Write the schema
        VersionJsonSchema.GenerateSchema(schemaPath);

        return Task.FromResult(0);
    }

    private static Task<int> DefaultHandler(string versionPath, bool onlineUpdate)
    {
        Log.Logger.Information("Writing defaults to file : Version Path: {VersionPath}, Online Updates: {OnlineUpdate}",
            versionPath, onlineUpdate);

        // Create defaults
        VersionJsonSchema versionSchema = new();
        versionSchema.SetDefaults();

        // Replace stable versions with online version information
        if (onlineUpdate)
        {
            Log.Logger.Information("Getting latest stable version information online...");
            versionSchema.GetOnlineUpdates();
        }

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

        // Load versions
        Log.Logger.Information("Reading versions from {Path}", versionPath);
        var versionJson = VersionJsonSchema.FromFile(versionPath);
        Log.Logger.Information("Loaded {Count} products from {Path}", versionJson.Products.Count, versionPath);
        foreach (var productVersion in versionJson.Products)
        {
            Log.Logger.Information(
                "Product: {Product}, Stable Version: {StableVersion}, Latest Version: {LatestVersion}",
                productVersion.Name, productVersion.Stable.Version, productVersion.Latest.Version);
        }

        // Replace stable versions with online version information
        if (onlineUpdate)
        {
            Log.Logger.Information("Getting latest stable version information online...");
            versionJson.GetOnlineUpdates();
        }

        // Create matrix
        MatrixJsonSchema matrixJson = new()
        {
            Images = BuildInfo.CreateImages(versionJson.Products)
        };
        Log.Logger.Information("Created {Count} images in matrix", matrixJson.Images.Count);
        foreach (var image in matrixJson.Images)
        {
            Log.Logger.Information("Name: {Name}, Branch: {Branch}, Tags: {Tags}, Args: {Args}", image.Name,
                image.Branch, image.Tags.Count, image.Args.Count);
        }

        // Write matrix
        Log.Logger.Information("Writing matrix to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixJson);

        return Task.FromResult(0);
    }
}