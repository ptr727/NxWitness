using CreateMatrix;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;

// Failure conditions will throw

// Configure serilog console logging
SelfLog.Enable(Console.Error);
LoggerConfiguration loggerConfiguration = new();
loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}");
Log.Logger = loggerConfiguration.CreateLogger();

// Create default versions and schema
// VersionJsonSchema.WriteDefaultsToFile(@"Version.json");
// VersionJsonSchema.GenerateSchema(@"Version.schema.json");

// Default file names
var versionPath = "Version.json";
var matrixPath = "Matrix.json";

// Replace defaults from commandline if set
if (args.Length == 2)
{
    versionPath = args[0];
    matrixPath = args[1];
}
else 
{
    Log.Logger.Information("Commandline not specified, using defaults, Usage: CreateMatrix [VersionPath] [MatrixPath]");
}
Log.Logger.Information("VersionPath: {VersionPath}, MatrixPath: {MatrixPath}", versionPath, matrixPath);

// Load versions
Log.Logger.Information("Reading versions from {Path}", versionPath);
var versionJson = VersionJsonSchema.FromFile(versionPath);
Log.Logger.Information("Loaded {Count} products from {Path}", versionJson.Products.Count, versionPath);
foreach (var productVersion in versionJson.Products)
{
    Log.Logger.Information("Product: {Product}, Stable Version: {StableVersion}, Latest Version: {LatestVersion}", productVersion.Name, productVersion.Stable.Version, productVersion.Latest.Version);
}

// Replace stable versions with online version information
Log.Logger.Information("Getting latest stable version information online...");
versionJson.GetOnlineVersions();

// Create matrix
MatrixJsonSchema matrixJson = new()
{
    Images = BuildInfo.CreateImages(versionJson.Products)
};
Log.Logger.Information("Created {Count} images in matrix", matrixJson.Images.Count);
foreach (var image in matrixJson.Images)
{
    Log.Logger.Information("Name: {Name}, Branch: {Branch}, Tags: {Tags}, Args: {Args}", image.Name, image.Branch, image.Tags.Count, image.Args.Count);
}

// Write matrix
Log.Logger.Information("Writing matrix to {Path}", matrixPath);
MatrixJsonSchema.ToFile(matrixPath, matrixJson);
