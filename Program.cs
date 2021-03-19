using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using ThunderstoreCLI.Commands;

namespace ThunderstoreCLI
{
    public class PackageOptions
    {
        [Option("config-path", Required = false, Default = "./thunderstore.toml", HelpText = "Path for the project configuration file")]
        public string ConfigPath { get; set; }

        [Option("package-name", Required = false, HelpText = "Name for the package")]
        public string Name { get; set; }

        [Option("package-namespace", Required = false, HelpText = "Namespace for the package")]
        public string Namespace { get; set; }

        [Option("package-version", Required = false, HelpText = "Verson number for the package")]
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

    [Verb("publish", HelpText = "Publish a package")]
    public class PublishOptions { }

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
