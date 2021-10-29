using System;
using System.Collections.Generic;
using CommandLine;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Options;

namespace ThunderstoreCLI
{
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
            return InitCommand.Run(GetConfig(new Config.CLIInitCommandConfig(options)));
        }

        static int Build(BuildOptions options)
        {
            return BuildCommand.Run(GetConfig(new Config.CLIBuildCommandConfig(options)));
        }

        static int Publish(PublishOptions options)
        {
            return PublishCommand.Run(GetConfig(new Config.CLIPublishCommandConfig(options)));
        }
    }

    class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }
}
