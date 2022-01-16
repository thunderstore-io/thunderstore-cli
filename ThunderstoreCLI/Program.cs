using CommandLine;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Options;

namespace ThunderstoreCLI;

internal static class Program
{
    private static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions>(args)
            .MapResult(
                (InitOptions o) => HandleParsed(o),
                (BuildOptions o) => HandleParsed(o),
                (PublishOptions o) => HandleParsed(o),
                _ => 1 // failure to parse
            );
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
