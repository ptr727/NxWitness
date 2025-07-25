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
    internal static void WriteFile(string fileName, string value)
    {
        // Always write as CRLF with newline at the end
        if (value.Contains('\n') && !value.Contains('\r'))
        {
            // Replace LF with CRLF
            value = value.Replace("\n", "\r\n");
        }
        value = value.TrimEnd() + "\r\n";
        File.WriteAllText(fileName, value);
    }

    private static async Task<int> Main(string[] args)
    {
        // Configure logger
        ConfigureLogger();

        // Create command line options
        RootCommand rootCommand = CreateCommandLine();

        // Run
        // 0 == ok, 1 == error
        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static RootCommand CreateCommandLine()
    {
        Option<string> versionOption = new("--version")
        {
            Description = "Version JSON file.",
            DefaultValueFactory = _ => "./Make/Version.json",
        };

        Option<string> matrixOption = new("--matrix")
        {
            Description = "Matrix JSON file.",
            DefaultValueFactory = _ => "./Make/Matrix.json",
        };

        Option<string> schemaVersionOption = new("--schemaversion")
        {
            Description = "Version JSON schema file.",
            DefaultValueFactory = _ => "./JSON/Version.schema.json",
        };

        Option<string> schemaMatrixOption = new("--schemamatrix")
        {
            Description = "Matrix JSON schema file.",
            DefaultValueFactory = _ => "./JSON/Matrix.schema.json",
        };

        Option<bool> updateOption = new("--update")
        {
            Description = "Update version information",
            DefaultValueFactory = _ => false,
        };

        Option<string> makeOption = new("--make")
        {
            Description = "Make directory.",
            DefaultValueFactory = _ => "./Make",
        };

        Option<string> dockerOption = new("--docker")
        {
            Description = "Docker directory.",
            DefaultValueFactory = _ => "./Docker",
        };

        Option<VersionInfo.LabelType> labelOption = new("--label")
        {
            Description = "Version label.",
            DefaultValueFactory = _ => VersionInfo.LabelType.Latest,
        };

        Command versionCommand = new("version", "Create version information file")
        {
            versionOption,
        };
        versionCommand.SetAction(parseResult =>
        {
            return VersionHandler(parseResult.GetRequiredValue(versionOption));
        });

        Command matrixCommand = new("matrix", "Create matrix information file")
        {
            versionOption,
            matrixOption,
            updateOption,
        };
        matrixCommand.SetAction(parseResult =>
        {
            return MatrixHandler(
                parseResult.GetRequiredValue(versionOption),
                parseResult.GetRequiredValue(matrixOption),
                parseResult.GetRequiredValue(updateOption)
            );
        });

        Command schemaCommand = new("schema", "Write Version and Matrix JSON schema files")
        {
            schemaVersionOption,
            schemaMatrixOption,
        };
        schemaCommand.SetAction(parseResult =>
        {
            return SchemaHandler(
                parseResult.GetRequiredValue(schemaVersionOption),
                parseResult.GetRequiredValue(schemaMatrixOption)
            );
        });

        Command makeCommand = new("make", "Create Docker and Compose files from Version file")
        {
            versionOption,
            makeOption,
            dockerOption,
            labelOption,
        };
        makeCommand.SetAction(parseResult =>
        {
            return MakeHandler(
                parseResult.GetRequiredValue(versionOption),
                parseResult.GetRequiredValue(makeOption),
                parseResult.GetRequiredValue(dockerOption),
                parseResult.GetRequiredValue(labelOption)
            );
        });

#pragma warning disable IDE0028 // Simplify collection initialization
        RootCommand rootCommand = new(
            "CreateMatrix utility to create a matrix of builds from product versions"
        )
        {
            versionCommand,
            matrixCommand,
            schemaCommand,
            makeCommand,
        };
#pragma warning restore IDE0028 // Simplify collection initialization
        return rootCommand;
    }

    private static void ConfigureLogger()
    {
        // Configure serilog console logging
        SelfLog.Enable(Console.Error);
        LoggerConfiguration loggerConfiguration = new();
        _ = loggerConfiguration.WriteTo.Console(
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture
        );
        Log.Logger = loggerConfiguration.CreateLogger();
    }

    private static Task<int> SchemaHandler(string schemaVersionPath, string schemaMatrixPath)
    {
        Log.Logger.Information(
            "Writing schema to file : Version Path: {VersionPath}, Matrix Path: {MatrixPath}",
            schemaVersionPath,
            schemaMatrixPath
        );
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

    private static Task<int> MatrixHandler(
        string versionPath,
        string matrixPath,
        bool updateVersion
    )
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
        MatrixJsonSchema matrixSchema = new()
        {
            Images = ImageInfo.CreateImages(fileSchema.Products),
        };
        Log.Logger.Information("Created {Count} images in matrix", matrixSchema.Images.Count);

        // Log info
        matrixSchema.Images.ForEach(item => item.LogInformation());

        // Write matrix
        Log.Logger.Information("Writing matrix information to {Path}", matrixPath);
        MatrixJsonSchema.ToFile(matrixPath, matrixSchema);

        return Task.FromResult(0);
    }

    private static Task<int> MakeHandler(
        string versionPath,
        string makePath,
        string dockerPath,
        VersionInfo.LabelType label
    )
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
