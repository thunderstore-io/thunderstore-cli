using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Tomlyn.Syntax;

namespace ThunderstoreCLI
{
    class Program
    {

        [Verb("init", HelpText = "Initialize a new project")]
        class InitOptions { }

        [Verb("build", HelpText = "Build a package")]
        class BuildOptions { }

        [Verb("publish", HelpText = "Publish a package")]
        class PublishOptions { }

        static void Main(string[] args)
        {
            var config = GetConfig();
            Parser.Default.ParseArguments<InitOptions, BuildOptions, PublishOptions>(args)
                .WithParsed<InitOptions>(o => Init(o))
                .WithParsed<BuildOptions>(o => Build(o))
                .WithParsed<PublishOptions>(o => Publish(o));
        }

        static Config.Config GetConfig()
        {
            return Config.Config.Parse(
                new Config.CLIParameterConfig(),
                new Config.EnvironmentConfig(),
                new Config.ProjectFileConfig(),
                new Config.UserFileConfig(),
                new Config.BaseConfig()
            );
        }

        static void Init(InitOptions options)
        {
            Console.WriteLine("Init");
        }

        static void Build(BuildOptions options)
        {
            Console.WriteLine("Build");
        }

        static void Publish(PublishOptions options)
        {
            Console.WriteLine("Publish");
        }
    }

    class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }
}
