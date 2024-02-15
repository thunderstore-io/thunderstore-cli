using CommandLine;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Utils;
using static Crayon.Output;

namespace ThunderstoreCLI;

/// Options are arguments passed from command line.
public abstract class BaseOptions
{
    [Option("tcli-directory", Required = false, HelpText = "Directory where TCLI keeps its data, %APPDATA%/ThunderstoreCLI on Windows and ~/.config/ThunderstoreCLI on Linux")]
    // will be initialized in Init if null
    public string TcliDirectory { get; set; } = null!;

    [Option("repository", Required = false, HelpText = "URL of the default repository")]
    public string Repository { get; set; } = null!;

    [Option("config-path", Required = false, Default = Defaults.PROJECT_CONFIG_PATH, HelpText = "Path for the project configuration file")]
    public string? ConfigPath { get; set; }

    public virtual void Init()
    {
        // ReSharper disable once ConstantNullCoalescingCondition
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        TcliDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ThunderstoreCLI");
    }

    public virtual bool Validate()
    {
        if (!Directory.Exists(TcliDirectory))
        {
            Directory.CreateDirectory(TcliDirectory);
        }

        return true;
    }

    public abstract int Execute();
}

public abstract class PackageOptions : BaseOptions
{
    [Option("package-name", SetName = "build", Required = false, HelpText = "Name for the package")]
    public string? Name { get; set; }

    [Option("package-namespace", SetName = "build", Required = false, HelpText = "Namespace for the package")]
    public string? Namespace { get; set; }

    [Option("package-version", SetName = "build", Required = false, HelpText = "Version number for the package")]
    public string? VersionNumber { get; set; }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            Write.ErrorExit("Invalid value for --config-path argument");
            return false;
        }

        var isInitCommand = this is InitOptions;
        var fullPath = Path.GetFullPath(ConfigPath);
        if (!isInitCommand && !File.Exists(fullPath))
        {
            Write.ErrorExit(
                $"Configuration file not found, looked from: {White(Dim(fullPath))}",
                "A project configuration file is required for this command.",
                "You can initialize one with the 'init' command or define its location with --config-path argument."
            );
            return false;
        }

        return true;
    }
}

[Verb("init", HelpText = "Initialize a new project configuration")]
public class InitOptions : PackageOptions
{
    public const string OVERWRITE_FLAG = "overwrite";

    [Option(OVERWRITE_FLAG, Required = false, Default = false, HelpText = "If present, overwrite current configuration")]
    public bool Overwrite { get; set; }

    public override int Execute()
    {
        return InitCommand.Run(Config.FromCLI(new CLIInitCommandConfig(this)));
    }
}

[Verb("build", HelpText = "Build a package")]
public class BuildOptions : PackageOptions
{
    public override int Execute()
    {
        return BuildCommand.Run(Config.FromCLI(new CLIBuildCommandConfig(this)));
    }
}

[Verb("publish", HelpText = "Publish a package. By default will also build a new package.")]
public class PublishOptions : PackageOptions
{
    [Option("file", SetName = "select", Required = false, HelpText = "If provided, use defined package instead of building.")]
    public string? File { get; set; }

    [Option("token", Required = false, HelpText = "Authentication token to use for publishing.")]
    public string? Token { get; set; }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        if (File is not null)
        {
            var filePath = Path.GetFullPath(File);
            if (!System.IO.File.Exists(filePath))
            {
                Write.ErrorExit(
                    $"Package file not found, looked from: {White(Dim(filePath))}",
                    "Package defined with --file argument must exist."
                );
                return false;
            }
        }

        return true;
    }

    public override int Execute()
    {
        return PublishCommand.Run(Config.FromCLI(new CLIPublishCommandConfig(this)));
    }
}

public abstract class ModManagementOptions : BaseOptions
{
    [Value(0, MetaName = "Game Name", Required = true, HelpText = "The identifier of the game to manage mods for")]
    public string GameName { get; set; } = null!;

    [Value(1, MetaName = "Package", Required = true, HelpText = "Path to package zip or package name in the format namespace-name(-version)")]
    public string Package { get; set; } = null!;

    [Option(HelpText = "Profile to install to", Default = "DefaultProfile")]
    public string? Profile { get; set; }

    protected enum CommandInner
    {
        Install,
        Uninstall
    }

    protected abstract CommandInner CommandType { get; }

    public override int Execute()
    {
        var config = Config.FromCLI(new ModManagementCommandConfig(this));
        return CommandType switch
        {
            CommandInner.Install => InstallCommand.Run(config).GetAwaiter().GetResult(),
            CommandInner.Uninstall => UninstallCommand.Run(config),
            _ => throw new NotSupportedException()
        };
    }
}

[Verb("install", HelpText = "Installs a mod to a profile")]
public class InstallOptions : ModManagementOptions
{
    protected override CommandInner CommandType => CommandInner.Install;
}

[Verb("uninstall", HelpText = "Uninstalls a mod from a profile")]
public class UninstallOptions : ModManagementOptions
{
    protected override CommandInner CommandType => CommandInner.Uninstall;
}

[Verb("import-game", HelpText = "Imports a new game to use with TCLI")]
public class GameImportOptions : BaseOptions
{
    [Option(HelpText = "Path to game exe to use when launching the game. Only works with servers.")]
    public required string? ExePath { get; set; }

    [Value(0, Required = true, HelpText = "The identifier for the game to import.")]
    public required string GameId { get; set; }

    public override bool Validate()
    {
        if (!string.IsNullOrWhiteSpace(ExePath) && !File.Exists(ExePath))
        {
            Write.ErrorExit($"Could not locate game exe at {ExePath}");
        }

        return base.Validate();
    }

    public override int Execute()
    {
        var config = Config.FromCLI(new GameImportCommandConfig(this));
        return ImportGameCommand.Run(config);
    }
}

[Verb("run", HelpText = "Run a game modded")]
public class RunGameOptions : BaseOptions
{
    [Value(0, MetaName = "Game", Required = true, HelpText = "The identifier of the game to run.")]
    public required string GameName { get; set; }

    [Option(HelpText = "Which profile to run the game under", Default = "DefaultProfile")]
    public required string Profile { get; set; }

    [Option(HelpText = "Arguments to run the game with. Anything after a trailing -- will be prioritized over this argument.")]
    public string? Args { get; set; }

    public override int Execute()
    {
        var config = Config.FromCLI(new RunGameCommandConfig(this));
        return RunCommand.Run(config);
    }
}

[Verb("list", HelpText = "List configured games, profiles, and mods")]
public class ListOptions : BaseOptions
{
    public override int Execute()
    {
        ListCommand.Run(Config.FromCLI(new ListConfig(this)));
        return 0;
    }
}
