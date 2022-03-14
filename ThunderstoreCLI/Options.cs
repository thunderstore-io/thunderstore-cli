using CommandLine;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Models.Interaction;
using static Crayon.Output;

namespace ThunderstoreCLI.Options;

/// Options are arguments passed from command line.
public abstract class BaseOptions
{
    [Option("output", Required = false, HelpText = "The output format for all output. Valid options are HUMAN and JSON.")]
    public InteractionOutputType OutputType { get; set; } = InteractionOutputType.HUMAN;

    [Option("tcli-directory", Required = false, HelpText = "Directory where TCLI keeps its data, %APPDATA%/ThunderstoreCLI on Windows and ~/.config/ThunderstoreCLI on Linux")]
    // will be initialized in Init if null
    public string TcliDirectory { get; set; } = null!;

    public virtual void Init()
    {
        InteractionOptions.OutputType = OutputType;

        // ReSharper disable once ConstantNullCoalescingCondition
        TcliDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ThunderstoreCLI");
    }

    public virtual bool Validate()
    {
        if (!Directory.Exists(TcliDirectory))
        {
            Directory.CreateDirectory(TcliDirectory!);
        }

        return true;
    }

    public abstract int Execute();
}

public abstract class PackageOptions : BaseOptions
{
    [Option("config-path", Required = false, Default = Defaults.PROJECT_CONFIG_PATH, HelpText = "Path for the project configuration file")]
    public string? ConfigPath { get; set; }

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
        return InitCommand.Run(Config.Config.FromCLI(new Config.CLIInitCommandConfig(this)));
    }
}

[Verb("build", HelpText = "Build a package")]
public class BuildOptions : PackageOptions
{
    public override int Execute()
    {
        return BuildCommand.Run(Config.Config.FromCLI(new Config.CLIBuildCommandConfig(this)));
    }
}

[Verb("publish", HelpText = "Publish a package. By default will also build a new package.")]
public class PublishOptions : PackageOptions
{
    [Option("file", SetName = "select", Required = false, HelpText = "If provided, use defined package instead of building.")]
    public string? File { get; set; }

    [Option("token", Required = false, HelpText = "Authentication token to use for publishing.")]
    public string? Token { get; set; }

    [Option("repository", Required = false, HelpText = "URL of the repository where to publish.")]
    public string? Repository { get; set; }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        if (!(File is null))
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
        return PublishCommand.Run(Config.Config.FromCLI(new Config.CLIPublishCommandConfig(this)));
    }
}
