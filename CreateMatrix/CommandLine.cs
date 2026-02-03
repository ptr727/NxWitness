using System.Collections.Frozen;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace CreateMatrix;

internal sealed class CommandLine
{
    private static readonly Option<string> s_versionPathOption = new Option<string>("--versionpath")
    {
        Description = "Version JSON file path.",
        DefaultValueFactory = _ => "./Make/Version.json",
    }.AcceptLegalFilePathsOnly();

    private static readonly Option<string> s_matrixPathOption = new Option<string>("--matrixpath")
    {
        Description = "Matrix JSON file path.",
        DefaultValueFactory = _ => "./Make/Matrix.json",
    }.AcceptLegalFilePathsOnly();

    private static readonly Option<string> s_versionSchemaPathOption = new Option<string>(
        "--versionschemapath"
    )
    {
        Description = "Version JSON schema file path.",
        DefaultValueFactory = _ => "./JSON/Version.schema.json",
    }.AcceptLegalFilePathsOnly();

    private static readonly Option<string> s_matrixSchemaPathOption = new Option<string>(
        "--matrixschemapath"
    )
    {
        Description = "Matrix JSON schema file path.",
        DefaultValueFactory = _ => "./JSON/Matrix.schema.json",
    }.AcceptLegalFilePathsOnly();

    private static readonly Option<bool> s_updateVersionOption = new("--updateversion")
    {
        Description = "Update version information from online sources.",
        DefaultValueFactory = _ => false,
    };

    private static readonly Option<DirectoryInfo> s_makeDirectoryOption = new Option<DirectoryInfo>(
        "--makedirectory"
    )
    {
        Description = "Make directory path.",
        DefaultValueFactory = _ => new DirectoryInfo("./Make"),
    }.AcceptExistingOnly();

    private static readonly Option<DirectoryInfo> s_dockerDirectoryOption =
        new Option<DirectoryInfo>("--dockerdirectory")
        {
            Description = "Docker directory path.",
            DefaultValueFactory = _ => new DirectoryInfo("./Docker"),
        }.AcceptExistingOnly();

    private static readonly Option<VersionInfo.LabelType> s_versionLabelOption = new(
        "--versionlabel"
    )
    {
        Description = "Version label to apply.",
        DefaultValueFactory = _ => VersionInfo.LabelType.Latest,
    };

    internal CommandLine(string[] args)
    {
        Root = CreateRootCommand();
        Result = Root.Parse(args, new ParserConfiguration { EnablePosixBundling = false });
        Result.InvocationConfiguration.EnableDefaultExceptionHandler = false;
    }

    private static readonly FrozenSet<string> s_cliBypassList = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "--help",
        "--version"
    );

    internal RootCommand Root { get; }
    internal ParseResult Result { get; }

    internal static RootCommand CreateRootCommand()
    {
        RootCommand rootCommand = new(
            "CreateMatrix utility to create a matrix of builds from online product versions"
        );
        rootCommand.Subcommands.Add(CreateVersionCommand());
        rootCommand.Subcommands.Add(CreateMatrixCommand());
        rootCommand.Subcommands.Add(CreateSchemaCommand());
        rootCommand.Subcommands.Add(CreateMakeCommand());
        return rootCommand;
    }

    internal static Command CreateVersionCommand()
    {
        Command versionCommand = new(
            "version",
            "Create version information file from online sources"
        )
        {
            s_versionPathOption,
        };
        versionCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(CreateOptions(parseResult), cancellationToken).ExecuteVersionAsync()
        );
        return versionCommand;
    }

    internal static Command CreateMatrixCommand()
    {
        s_versionPathOption.Validators.Add(result =>
        {
            string? value = result.GetValueOrDefault<string>();
            if (string.IsNullOrWhiteSpace(value) || !File.Exists(value))
            {
                result.AddError($"File does not exist: {value}");
            }
        });

        Command matrixCommand = new("matrix", "Create matrix information file from online sources")
        {
            s_versionPathOption,
            s_matrixPathOption,
            s_updateVersionOption,
        };
        matrixCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(CreateOptions(parseResult), cancellationToken).ExecuteMatrixAsync()
        );
        return matrixCommand;
    }

    internal static Command CreateSchemaCommand()
    {
        Command schemaCommand = new("schema", "Create Version and Matrix JSON schema files")
        {
            s_versionSchemaPathOption,
            s_matrixSchemaPathOption,
        };
        schemaCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(CreateOptions(parseResult), cancellationToken).ExecuteSchemaAsync()
        );
        return schemaCommand;
    }

    internal static Command CreateMakeCommand()
    {
        Command makeCommand = new("make", "Create Docker and Compose files from version file")
        {
            s_versionPathOption,
            s_makeDirectoryOption,
            s_dockerDirectoryOption,
            s_versionLabelOption,
        };
        makeCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(CreateOptions(parseResult), cancellationToken).ExecuteMakeAsync()
        );
        return makeCommand;
    }

    internal static Options CreateOptions(ParseResult parseResult) =>
        new()
        {
            VersionPath = parseResult.GetValue(s_versionPathOption) ?? string.Empty,
            MatrixPath = parseResult.GetValue(s_matrixPathOption) ?? string.Empty,
            VersionSchemaPath = parseResult.GetValue(s_versionSchemaPathOption) ?? string.Empty,
            MatrixSchemaPath = parseResult.GetValue(s_matrixSchemaPathOption) ?? string.Empty,
            UpdateVersion = parseResult.GetValue(s_updateVersionOption),
            MakeDirectory = parseResult.GetValue(s_makeDirectoryOption)!,
            DockerDirectory = parseResult.GetValue(s_dockerDirectoryOption)!,
            VersionLabel = parseResult.GetValue(s_versionLabelOption),
        };

    internal static bool BypassStartup(ParseResult parseResult) =>
        parseResult.Errors.Count > 0
        || parseResult.CommandResult.Children.Any(symbolResult =>
            symbolResult is OptionResult optionResult
            && s_cliBypassList.Contains(optionResult.Option.Name)
        );

    internal sealed class Options
    {
        internal required string VersionPath { get; init; }
        internal required string MatrixPath { get; init; }
        internal required string VersionSchemaPath { get; init; }
        internal required string MatrixSchemaPath { get; init; }
        internal required bool UpdateVersion { get; init; }
        internal required DirectoryInfo MakeDirectory { get; init; }
        internal required DirectoryInfo DockerDirectory { get; init; }
        internal required VersionInfo.LabelType VersionLabel { get; init; }
    }
}
