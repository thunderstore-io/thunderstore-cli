using System.Diagnostics;
using CommandLine;
using ThunderstoreCLI.Options;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI;

internal static class Program
{
    private static int Main(string[] args)
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("TCLI_WAIT_DEBUGGER") == "1")
            while (!Debugger.IsAttached)
            { }
#endif

        var updateChecker = UpdateChecker.CheckForUpdates();
        var exitCode = Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions, InstallOptions, UninstallOptions>(args)
            .MapResult(
                (InitOptions o) => HandleParsed(o),
                (BuildOptions o) => HandleParsed(o),
                (PublishOptions o) => HandleParsed(o),
                (InstallOptions o) => HandleParsed(o),
                (UninstallOptions o) => HandleParsed(o),
                _ => 1 // failure to parse
            );
        UpdateChecker.WriteUpdateNotification(updateChecker);
        return exitCode;
    }

    private static int HandleParsed(BaseOptions parsed)
    {
        parsed.Init();
        if (!parsed.Validate())
        {
            return 1;
        }
        try
        {
            return parsed.Execute();
        }
        catch (CommandFatalException cfe)
        {
            Write.Error(cfe.ErrorMessage);
#if DEBUG
            throw;
#else
            return 1;
#endif
        }
    }
}

internal class CommandException : Exception
{
    public CommandException(string message) : base(message) { }
}
