using System;
using static Crayon.Output;

namespace ThunderstoreCLI.Config
{
    public abstract class CLIParameterConfig<T> : EmptyConfig
    {
        protected T options;

        public CLIParameterConfig(T options)
        {
            this.options = options;
        }
    }

    public class CLIInitCommandConfig : CLIParameterConfig<InitOptions>
    {
        public CLIInitCommandConfig(InitOptions options) : base(options) { }

        public override GeneralConfig GetGeneralConfig()
        {
            return new GeneralConfig()
            {
                ProjectConfigPath = options.ConfigPath
            };
        }

        public override PackageMeta GetPackageMeta()
        {
            if (options == null) return null;
            return new PackageMeta()
            {
                Namespace = options.Namespace,
                Name = options.Name,
                VersionNumber = options.VersionNumber
            };
        }
    }

    public class CLIBuildCommandConfig : CLIParameterConfig<BuildOptions>
    {
        public CLIBuildCommandConfig(BuildOptions options) : base(options) { }

        public override GeneralConfig GetGeneralConfig()
        {
            return new GeneralConfig()
            {
                ProjectConfigPath = options.ConfigPath
            };
        }

        public override PackageMeta GetPackageMeta()
        {
            if (options == null) return null;
            return new PackageMeta()
            {
                Namespace = options.Namespace,
                Name = options.Name,
                VersionNumber = options.VersionNumber
            };
        }
    }

    public class CLIPublishCommandConfig : CLIParameterConfig<PublishOptions>
    {
        public CLIPublishCommandConfig(PublishOptions options) : base(options) { }

        public override GeneralConfig GetGeneralConfig()
        {
            return new GeneralConfig()
            {
                ProjectConfigPath = options.ConfigPath
            };
        }

        public override PackageMeta GetPackageMeta()
        {
            if (options == null) return null;
            return new PackageMeta()
            {
                Namespace = options.Namespace,
                Name = options.Name,
                VersionNumber = options.VersionNumber
            };
        }

        public override PublishConfig GetPublishConfig()
        {
            return new PublishConfig()
            {
                Repository = options.Repository
            };
        }

        public override AuthConfig GetAuthConfig()
        {
            if (options.UseSessionAuth)
            {
                Console.WriteLine(Yellow("The usage of session auth is deprecated and will be removed in the future without warning!"));
            }
            return new AuthConfig()
            {
                DefaultToken = options.Token,
                UseSessionAuth = options.UseSessionAuth
            };
        }
    }
}
