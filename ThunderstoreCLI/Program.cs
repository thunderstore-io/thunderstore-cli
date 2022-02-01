using System.Diagnostics;
using CommandLine;
using ThunderstoreCLI.Options;

namespace ThunderstoreCLI;

internal static class Program
{
    private static int Main(string[] args)
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("TCLI_WAIT_DEBUGGER") is not null)
            while (!Debugger.IsAttached)
            { }
#endif

        var updateChecker = UpdateChecker.CheckForUpdates();
        var exitCode = Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions, InstallOptions>(args)
            .MapResult(
                (InitOptions o) => HandleParsed(o),
                (BuildOptions o) => HandleParsed(o),
                (PublishOptions o) => HandleParsed(o),
                (InstallOptions o) => HandleParsed(o),
                _ => 1 // failure to parse
            );
        UpdateChecker.WriteUpdateNotification(updateChecker);
        return exitCode;
    }

    private static int HandleParsed(PackageOptions parsed)
    {
        parsed.Init();
        if (!parsed.Validate())
            return 1;
        return parsed.Execute();
    }
}

class CommandException : Exception
{
    public CommandException(string message) : base(message) { }
}
