using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using ThunderstoreCLI.Commands;

namespace ThunderstoreCLI
{
    public class PackageOptions
    {
        [Option("config-path", Required = false, Default = Defaults.PROJECT_CONFIG_PATH, HelpText = "Path for the project configuration file")]
        public string ConfigPath { get; set; }

        [Option("package-name", SetName = "build", Required = false, HelpText = "Name for the package")]
        public string Name { get; set; }

        [Option("package-namespace", SetName = "build", Required = false, HelpText = "Namespace for the package")]
        public string Namespace { get; set; }

        [Option("package-version", SetName = "build", Required = false, HelpText = "Verson number for the package")]
        public string VersionNumber { get; set; }
    }

    [Verb("init", HelpText = "Initialize a new project configuration")]
    public class InitOptions : PackageOptions
    {
        public const string OVERWRITE_FLAG = "overwrite";

        [Option(OVERWRITE_FLAG, Required = false, Default = false, HelpText = "If present, overwrite current configuration")]
        public bool Overwrite { get; set; }
    }

    [Verb("build", HelpText = "Build a package")]
    public class BuildOptions : PackageOptions { }

    [Verb("publish", HelpText = "Publish a package. By default will also build the project.")]
    public class PublishOptions : PackageOptions
    {
        [Option("file", SetName = "select", Required = false, HelpText = "If provided, defined file instead of building.")]
        public string File { get; set; }

        [Option("token", Required = false, HelpText = "Authentication token to use for publishing.")]
        public string Token { get; set; }

        [Option("repository", Required = false, HelpText = "URL of the repository where to publish.")]
        public string Repository { get; set; }
        
        [Option("use-session-auth", Default = false, Required = false, HelpText = "Use session auth instead of bearer auth. !!THIS WILL BE DEPRECATED!!")]
        public bool UseSessionAuth { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions>(args)
                .MapResult(
                    (InitOptions o) => Init(o),
                    (BuildOptions o) => Build(o),
                    (PublishOptions o) => Publish(o),
                    errs => HandleError(errs)
                );
        }

        static Config.Config GetConfig(Config.IConfigProvider cliConfig)
        {
            return Config.Config.Parse(
                cliConfig,
                new Config.EnvironmentConfig(),
                new Config.ProjectFileConfig(),
                new Config.UserFileConfig(),
                new Config.BaseConfig()
            );
        }

        static int HandleError(IEnumerable<Error> errors)
        {
            return 1;
        }

        static int Init(InitOptions options)
        {
            return InitCommand.Run(options, GetConfig(new Config.CLIInitCommandConfig(options)));
        }

        static int Build(BuildOptions options)
        {
            return BuildCommand.Run(options, GetConfig(new Config.CLIBuildCommandConfig(options)));
        }

        static int Publish(PublishOptions options)
        {
            return PublishCommand.Run(options, GetConfig(new Config.CLIPublishCommandConfig(options)));
        }
    }

    class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }
}
