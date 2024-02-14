using System.Diagnostics;
using CommandLine;
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

        string? trailingArgs = null;

        var argsDelimeterIndex = Array.IndexOf(args, "--");
        if (argsDelimeterIndex != -1)
        {
            var trailingWithQuotes = args.Skip(argsDelimeterIndex + 1).Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg);
            trailingArgs = string.Join(' ', trailingWithQuotes);
            args = args[..argsDelimeterIndex];
        }

        var updateChecker = UpdateChecker.CheckForUpdates();
        var exitCode = Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions
#if INSTALLERS
                , InstallOptions, UninstallOptions, GameImportOptions, RunGameOptions, ListOptions
#endif
            >(args)
            .MapResult(
                (InitOptions o) => HandleParsed(o),
                (BuildOptions o) => HandleParsed(o),
                (PublishOptions o) => HandleParsed(o),
#if INSTALLERS
                (InstallOptions o) => HandleParsed(o),
                (UninstallOptions o) => HandleParsed(o),
                (GameImportOptions o) => HandleParsed(o),
                (RunGameOptions o) =>
                {
                    if (trailingArgs != null)
                    {
                        o.Args = trailingArgs;
                    }
                    return HandleParsed(o);
                },

                (ListOptions o) => HandleParsed(o),
#endif
                _ => 1 // failure to parse
            );
        UpdateChecker.WriteUpdateNotification(updateChecker);
        return exitCode;
    }

    // TODO: replace return codes with exceptions completely
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
