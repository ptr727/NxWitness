using CreateMatrix;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;
using System.IO;
using System.Xml.Linq;

// Configure serilog console logging
SelfLog.Enable(Console.Error);
LoggerConfiguration loggerConfiguration = new();
loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}");
Log.Logger = loggerConfiguration.CreateLogger();

// Create default versions and schema
// VersionJsonSchema.WriteDefaultsToFile(@"Version.json");
// VersionJsonSchema.GenerateSchema(@"Version.schema.json");

// Default file names
string versionPath = @"Version.json";
string matrixPath = @"Matrix.json";

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
Log.Logger.Information("VersionPath: {versionpath}, MatrixPath: {matrixpath}", versionPath, matrixPath);

// Load versions
var versionJson = VersionJsonSchema.FromFile(versionPath);
Log.Logger.Information("Loaded {count} products from {path}", versionJson.Products.Count, versionPath);

// Create images
MatrixJsonSchema matrixJson = new();
matrixJson.Images = BuildInfo.CreateImages(versionJson.Products);

// Write matrix
Log.Logger.Information("Writing {count} images to {path}", matrixJson.Images.Count, matrixPath);
MatrixJsonSchema.ToFile(matrixPath, matrixJson);
