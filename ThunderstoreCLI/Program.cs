using CommandLine;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Options;

namespace ThunderstoreCLI;

class Program
{
    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions>(args)
            .MapResult(
                (InitOptions o) => o.Validate() ? Init(o) : 1,
                (BuildOptions o) => o.Validate() ? Build(o) : 1,
                (PublishOptions o) => o.Validate() ? Publish(o) : 1,
                errs => HandleError(errs)
            );
    }

    static Config.Config GetConfig(Config.IConfigProvider cliConfig)
    {
        return Config.Config.Parse(
            cliConfig,
            new Config.EnvironmentConfig(),
            new Config.ProjectFileConfig(),
            new Config.BaseConfig()
        );
    }

    static int HandleError(IEnumerable<Error> errors)
    {
        return 1;
    }

    static int Init(InitOptions options)
    {
        var updateChecker = CheckForUpdates();
        var exitCode = InitCommand.Run(GetConfig(new Config.CLIInitCommandConfig(options)));
        WriteUpdateNotification(updateChecker);
        return exitCode;
    }

    static int Build(BuildOptions options)
    {
        var updateChecker = CheckForUpdates();
        var exitCode = BuildCommand.Run(GetConfig(new Config.CLIBuildCommandConfig(options)));
        WriteUpdateNotification(updateChecker);
        return exitCode;
    }

    static int Publish(PublishOptions options)
    {
        var updateChecker = CheckForUpdates();
        var exitCode = PublishCommand.Run(GetConfig(new Config.CLIPublishCommandConfig(options)));
        WriteUpdateNotification(updateChecker);
        return exitCode;
    }

    private static async Task<string> CheckForUpdates()
    {
        var current = MiscUtils.GetCurrentVersion();
        int[] latest;

        try
        {
            var responseContent = await MiscUtils.FetchReleaseInformation();
            latest = MiscUtils.ParseLatestVersion(responseContent);
        }
        catch (Exception)
        {
            return "";
        }

        if (
            latest[0] > current[0] ||
            (latest[0] == current[0] && latest[1] > current[1]) ||
            (latest[0] == current[0] && latest[1] == current[1] && latest[2] > current[2])
        )
        {
            var version = $"{latest[0]}.{latest[1]}.{latest[2]}";
            return $"Newer version {version} of Thunderstore CLI is available";
        }

        return "";
    }

    private static void WriteUpdateNotification(Task<string> checkTask)
    {
        if (!checkTask.IsCompleted)
        {
            return;
        }

        var notification = checkTask.GetAwaiter().GetResult();

        if (notification != "")
        {
            Write.Note(notification);
        }
    }
}

class CommandException : Exception
{
    public CommandException(string message) : base(message) { }
}
