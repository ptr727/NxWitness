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
        versionCommand.SetHandler((versionValue) => 
            { 
                return VersionHandler(versionValue); 
            },
            versionOption);

        var matrixCommand = new Command("matrix", "Create matrix information file")
            {
                versionOption,
                matrixOption,
                updateOption
            };
        matrixCommand.SetHandler((versionValue, matrixValue, updateValue) => 
            { 
                return MatrixHandler(versionValue, matrixValue, updateValue);
            },
            versionOption, matrixOption, updateOption);

        var schemaCommand = new Command("schema", "Write version and matrix schema files")
            {
                schemaVersionOption,
                schemaMatrixOption
            };
        schemaCommand.SetHandler((versionValue, matrixValue) => 
            { 
                return SchemaHandler(versionValue, matrixValue); 
            },
            schemaVersionOption, schemaMatrixOption);

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

    private static Task<int> VersionHandler(string versionPath)
    {
        // Get versions for all products using releases API
        VersionJsonSchema versionSchema = new();
        Log.Logger.Information("Getting online version information...");
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
        var fileSchema = VersionJsonSchema.FromFile(versionPath);
        fileSchema.Products.ForEach(item => item.LogInformation());
        fileSchema.Products.ForEach(item => item.VerifyUrls());

        // Update version information
        if (updateVersion)
        {
            // Get versions for all products using releases API
            VersionJsonSchema onlineSchema = new();
            Log.Logger.Information("Getting online version information...");
            onlineSchema.Products = ProductInfo.GetProducts();
            onlineSchema.Products.ForEach(item => item.GetVersions());
            onlineSchema.Products.ForEach(item => item.LogInformation());
            onlineSchema.Products.ForEach(item => item.VerifyUrls());

            // The online versions may not be older than the file versions
    
            // Iterate over all file products
            foreach (var fileProduct in fileSchema.Products)
            {
                // Find the matching online product
                var onlineProduct = onlineSchema.Products.Find(item => item.Product == fileProduct.Product);
                ArgumentNullException.ThrowIfNull(onlineProduct);

                // Find the version by known labels, label may not always be present
                foreach (var label in VersionUri.KnownLabels)
                {
                    var fileVersion = fileProduct.Versions.FirstOrDefault(item => item.Labels.Contains(label));
                    var onlineVersion = onlineProduct.Versions.FirstOrDefault(item => item.Labels.Contains(label));

                    if (fileVersion != null && onlineVersion != null)
                    {
                        // Compare the versions
                        var compare = onlineVersion.CompareTo(fileVersion);
                        if (compare < 0)
                        {
                            // Versions may not regress
                            Log.Logger.Error("{Label} : Online version {OnlineVersion} is less than file version {FileVersion}", label, onlineVersion.Version, fileVersion.Version);
                            return Task.FromResult(1);
                        }
                        else if (compare > 0)
                        {
                            // Newer online version
                            Log.Logger.Information("{Label} : Online version {OnlineVersion} is greater than file version {FileVersion}", label, onlineVersion.Version, fileVersion.Version);
                        }
                    }
                }
            }

            // Update the file version with the online version
            Log.Logger.Information("Writing version information to {Path}", versionPath);
            VersionJsonSchema.ToFile(versionPath, onlineSchema);
            fileSchema = onlineSchema;
        }

        // Remove all stable labels
        // https://github.com/ptr727/NxWitness/issues/62
        fileSchema.Products.ForEach(product =>
            product.Versions.ForEach(version =>
                version.Labels.RemoveAll(label => string.Equals(label, VersionUri.StableLabel))));

        // Create matrix
        Log.Logger.Information("Creating Matrix from versions");
        MatrixJsonSchema matrixSchema = new()
        {
            Images = ImageInfo.CreateImages(fileSchema.Products)
        };
        Log.Logger.Information("Created {Count} images in matrix", matrixSchema.Images.Count);

       // Log info
        matrixSchema.Images.ForEach(item => item.LogInformation());

        // Write matrix
        Log.Logger.Information("Writing matrix to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixSchema);

        return Task.FromResult(0);
    }
}