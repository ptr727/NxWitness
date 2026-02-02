using System.CommandLine;

namespace CreateMatrix;

internal static class CommandLine
{
    internal static RootCommand CreateRootCommand()
    {
        static Option<T> CreateOption<T>(string name, string description, Func<T> defaultValue)
        {
            Option<T> option = new(name, [])
            {
                Description = description,
                DefaultValueFactory = _ => defaultValue(),
            };
            return option;
        }

        Option<string> versionOption = CreateOption(
            "--version",
            "Version JSON file.",
            () => "./Make/Version.json"
        );

        Option<string> matrixOption = CreateOption(
            "--matrix",
            "Matrix JSON file.",
            () => "./Make/Matrix.json"
        );

        Option<string> schemaVersionOption = CreateOption(
            "--schemaversion",
            "Version JSON schema file.",
            () => "./JSON/Version.schema.json"
        );

        Option<string> schemaMatrixOption = CreateOption(
            "--schemamatrix",
            "Matrix JSON schema file.",
            () => "./JSON/Matrix.schema.json"
        );

        Option<bool> updateOption = CreateOption(
            "--update",
            "Update version information",
            () => false
        );

        Option<string> makeOption = CreateOption("--make", "Make directory.", () => "./Make");

        Option<string> dockerOption = CreateOption(
            "--docker",
            "Docker directory.",
            () => "./Docker"
        );

        Option<VersionInfo.LabelType> labelOption = CreateOption(
            "--label",
            "Version label.",
            () => VersionInfo.LabelType.Latest
        );

        Command versionCommand = new("version", "Create version information file")
        {
            versionOption,
        };
        versionCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(cancellationToken).VersionHandler(parseResult.GetValue(versionOption)!)
        );

        Command matrixCommand = new("matrix", "Create matrix information file")
        {
            versionOption,
            matrixOption,
            updateOption,
        };
        matrixCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(cancellationToken).MatrixHandler(
                    parseResult.GetValue(versionOption)!,
                    parseResult.GetValue(matrixOption)!,
                    parseResult.GetValue(updateOption)
                )
        );

        Command schemaCommand = new("schema", "Write Version and Matrix JSON schema files")
        {
            schemaVersionOption,
            schemaMatrixOption,
        };
        schemaCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(cancellationToken).SchemaHandler(
                    parseResult.GetValue(schemaVersionOption)!,
                    parseResult.GetValue(schemaMatrixOption)!
                )
        );

        Command makeCommand = new("make", "Create Docker and Compose files from Version file")
        {
            versionOption,
            makeOption,
            dockerOption,
            labelOption,
        };
        makeCommand.SetAction(
            (parseResult, cancellationToken) =>
                new Program(cancellationToken).MakeHandler(
                    parseResult.GetValue(versionOption)!,
                    parseResult.GetValue(makeOption)!,
                    parseResult.GetValue(dockerOption)!,
                    parseResult.GetValue(labelOption)
                )
        );

        RootCommand rootCommand = new(
            "CreateMatrix utility to create a matrix of builds from product versions"
        )
        {
            versionCommand,
            matrixCommand,
            schemaCommand,
            makeCommand,
        };
        return rootCommand;
    }
}
