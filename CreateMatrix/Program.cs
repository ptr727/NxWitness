using System.CommandLine;
using System.Globalization;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;

// dotnet publish --self-contained false --output ./publish
// ./publish/CreateMatrix matrix --update --matrix ./JSON/Matrix.json --version ./JSON/Version.json
// echo $?

namespace CreateMatrix;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Configure logger
        ConfigureLogger();

        // Create command line options
        RootCommand rootCommand = CreateCommandLine();

        // Run
        // 0 == ok, 1 == error
        return await rootCommand.InvokeAsync(args);
    }

    private static RootCommand CreateCommandLine()
    {
        Option<string> versionOption = new(
            name: "--version",
            description: "Version JSON file.",
            getDefaultValue: () => "./Make/Version.json");

        Option<string> matrixOption = new(
            name: "--matrix",
            description: "Matrix JSON file.",
            getDefaultValue: () => "./Make/Matrix.json");

        Option<string> schemaVersionOption = new(
            name: "--schemaversion",
            description: "Version JSON schema file.",
            getDefaultValue: () => "./JSON/Version.schema.json");

        Option<string> schemaMatrixOption = new(
            name: "--schemamatrix",
            description: "Matrix JSON schema file.",
            getDefaultValue: () => "./JSON/Matrix.schema.json");

        Option<bool> updateOption = new(
            name: "--update",
            description: "Update version information",
            getDefaultValue: () => false);

        Option<string> makeOption = new(
            name: "--make",
            description: "Make directory.",
            getDefaultValue: () => "./Make");

        Option<string> dockerOption = new(
            name: "--docker",
            description: "Docker directory.",
            getDefaultValue: () => "./Docker");

        Option<VersionInfo.LabelType> labelOption = new(
            name: "--label",
            description: "Version label.",
            getDefaultValue: () => VersionInfo.LabelType.Latest);

        Command versionCommand = new("version", "Create version information file")
            {
                versionOption
            };
        versionCommand.SetHandler(VersionHandler, versionOption);

        Command matrixCommand = new("matrix", "Create matrix information file")
            {
                versionOption,
                matrixOption,
                updateOption
            };
        matrixCommand.SetHandler(MatrixHandler, versionOption, matrixOption, updateOption);

        Command schemaCommand = new("schema", "Write Version and Matrix JSON schema files")
            {
                schemaVersionOption,
                schemaMatrixOption
            };
        schemaCommand.SetHandler(SchemaHandler, schemaVersionOption, schemaMatrixOption);

        Command makeCommand = new("make", "Create Docker and Compose files from Version file")
            {
                versionOption,
                makeOption,
                dockerOption,
                labelOption
            };
        makeCommand.SetHandler(MakeHandler, versionOption, makeOption, dockerOption, labelOption);

        RootCommand rootCommand = new("CreateMatrix utility to create a matrix of builds from product versions")
            {
                versionCommand,
                matrixCommand,
                schemaCommand,
                makeCommand
            };
        return rootCommand;
    }

    private static void ConfigureLogger()
    {
        // Configure serilog console logging
        SelfLog.Enable(Console.Error);
        LoggerConfiguration loggerConfiguration = new();
        _ = loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}", formatProvider: CultureInfo.InvariantCulture);
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
        Log.Logger.Information("Getting version information online...");
        versionSchema.Products = ProductInfo.GetProducts();
        versionSchema.Products.ForEach(item => item.GetVersions());
        versionSchema.Products.ForEach(item => item.LogInformation());
        versionSchema.Products.ForEach(item => item.VerifyUrls());

        // Write to file
        Log.Logger.Information("Writing version information to {Path}", versionPath);
        VersionJsonSchema.ToFile(versionPath, versionSchema);

        return Task.FromResult(0);
    }

    private static Task<int> MatrixHandler(string versionPath, string matrixPath, bool updateVersion)
    {
        // Load version info from file
        Log.Logger.Information("Reading version information from {Path}", versionPath);
        VersionJsonSchema fileSchema = VersionJsonSchema.FromFile(versionPath);

        // Re-verify as rules may have changed after file was written
        fileSchema.Products.ForEach(item => item.Verify());
        fileSchema.Products.ForEach(item => item.LogInformation());

        // Update version information
        if (updateVersion)
        {
            // Get versions for all products using releases API
            VersionJsonSchema onlineSchema = new();
            Log.Logger.Information("Getting version information online...");
            onlineSchema.Products = ProductInfo.GetProducts();
            onlineSchema.Products.ForEach(item => item.GetVersions());
            onlineSchema.Products.ForEach(item => item.LogInformation());

            // Make sure the labelled version numbers do not regress
            ReleaseVersionForward.Verify(fileSchema.Products, onlineSchema.Products);

            // Verify URL's
            onlineSchema.Products.ForEach(item => item.VerifyUrls());

            // Update the file version with the online version
            Log.Logger.Information("Writing version information to {Path}", versionPath);
            VersionJsonSchema.ToFile(versionPath, onlineSchema);
            fileSchema = onlineSchema;
        }
        else
        {
            // Verify URL's
            fileSchema.Products.ForEach(item => item.VerifyUrls());
        }

        // Create matrix
        Log.Logger.Information("Creating Matrix from versions");
        MatrixJsonSchema matrixSchema = new() { Images = ImageInfo.CreateImages(fileSchema.Products) };
        Log.Logger.Information("Created {Count} images in matrix", matrixSchema.Images.Count);

        // Log info
        matrixSchema.Images.ForEach(item => item.LogInformation());

        // Write matrix
        Log.Logger.Information("Writing matrix information to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixSchema);

        return Task.FromResult(0);
    }

    private static Task<int> MakeHandler(string versionPath, string makePath, string dockerPath, VersionInfo.LabelType label)
    {
        // Load version info from file
        Log.Logger.Information("Reading version information from {Path}", versionPath);
        VersionJsonSchema versionSchema = VersionJsonSchema.FromFile(versionPath);

        // Create Compose files
        ComposeFile.Create(makePath);

        // Create Docker files
        DockerFile.Create(versionSchema.Products, dockerPath, label);

        return Task.FromResult(0);
    }
}
