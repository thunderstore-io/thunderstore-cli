using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
