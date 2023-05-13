using System.CommandLine;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;

// dotnet publish --self-contained false --output ./publish
// ./publish/CreateMatrix matrix --update --matrix ./JSON/Matrix.json --version ./JSON/Version.json
// echo $?

namespace CreateMatrix;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Configure logger
        ConfigureLogger();

        // Create command line options
        var rootCommand = CreateCommandLine();

        // Run
        // 0 == ok, 1 == error
        return await rootCommand.InvokeAsync(args);
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

        var updateOption = new Option<bool>(
            name: "--update",
            description: "Update version information",
            getDefaultValue: () => false);

        var versionCommand = new Command("version", "Create version information file")
            {
                versionOption
            };
        versionCommand.SetHandler(VersionHandler, versionOption);

        var matrixCommand = new Command("matrix", "Create matrix information file")
            {
                versionOption,
                matrixOption,
                updateOption
            };
        matrixCommand.SetHandler(MatrixHandler, versionOption, matrixOption, updateOption);

        var schemaCommand = new Command("schema", "Write version and matrix schema files")
            {
                schemaVersionOption,
                schemaMatrixOption
            };
        schemaCommand.SetHandler(SchemaHandler, schemaVersionOption, schemaMatrixOption);

        var rootCommand = new RootCommand("CreateMatrix utility to create a matrix of builds from a list of product versions")
            {
                versionCommand,
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
        loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}");
        Log.Logger = loggerConfiguration.CreateLogger();
    }

    private static Task<int> SchemaHandler(string schemaVersionPath, string schemaMatrixPath)
    {
        Log.Logger.Information("Writing schema to file : Version Path: {VersionPath}, Matrix Path: {MatrixPath}", schemaVersionPath, schemaMatrixPath);
        VersionJsonSchema.GenerateSchema(schemaVersionPath);
        MatrixJsonSchema.GenerateSchema(schemaMatrixPath);
        return Task.FromResult(0);
    }

    private static Task<int> VersionHandler(string versionPath)
    {
        // Get versions for all products using releases API
        VersionJsonSchema versionSchema = new();
        Log.Logger.Information("Getting online version information...");
        versionSchema.Products = ProductInfo.GetProducts();
        versionSchema.Products.ForEach(item => item.GetVersions());
        versionSchema.Products.ForEach(item => item.LogInformation());
        versionSchema.Products.ForEach(item => item.VerifyUrls());

        // Filter using default rule set
        if (!VersionRule.Evaluate(versionSchema.Products, VersionRule.DefaultRuleList, true))
            Log.Logger.Warning("Version filter applied using default rules");

        // Write to file
        Log.Logger.Information("Writing version information to {Path}", versionPath);
        VersionJsonSchema.ToFile(versionPath, versionSchema);

        return Task.FromResult(0);
    }

    private static Task<int> MatrixHandler(string versionPath, string matrixPath, bool updateVersion)
    {
        // Load version info from file
        Log.Logger.Information("Reading version information from {Path}", versionPath);
        var fileSchema = VersionJsonSchema.FromFile(versionPath);
        fileSchema.Products.ForEach(item => item.LogInformation());

        // Filter using default rule set
        if (!VersionRule.Evaluate(fileSchema.Products, VersionRule.DefaultRuleList, true))
            Log.Logger.Warning("Version filter applied using default rules");

        // Update version information
        if (updateVersion)
        {
            // Get versions for all products using releases API
            VersionJsonSchema onlineSchema = new();
            Log.Logger.Information("Getting online version information...");
            onlineSchema.Products = ProductInfo.GetProducts();
            onlineSchema.Products.ForEach(item => item.GetVersions());
            onlineSchema.Products.ForEach(item => item.LogInformation());

            // Create a filter from the file information
            var fileFilter = VersionRule.Create(fileSchema.Products);

            // Filter online information using file information
            if (!VersionRule.Evaluate(fileSchema.Products, fileFilter, true))
                Log.Logger.Warning("Version filter applied using versions on file");

            // Update the file version with the online version
            onlineSchema.Products.ForEach(item => item.VerifyUrls());
            Log.Logger.Information("Writing version information to {Path}", versionPath);
            VersionJsonSchema.ToFile(versionPath, onlineSchema);
            fileSchema = onlineSchema;
        }

        // Create matrix
        Log.Logger.Information("Creating Matrix from versions");
        MatrixJsonSchema matrixSchema = new() { Images = ImageInfo.CreateImages(fileSchema.Products) };
        Log.Logger.Information("Created {Count} images in matrix", matrixSchema.Images.Count);

       // Log info
        matrixSchema.Images.ForEach(item => item.LogInformation());

        // Write matrix
        Log.Logger.Information("Writing matrix to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixSchema);

        return Task.FromResult(0);
    }
}