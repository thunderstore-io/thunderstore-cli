using System;
using System.Collections.Generic;
using System.Text;

namespace ThunderstoreCLI.Config
{
    public class CLIParameterConfig : IConfigProvider
    {
        public void Parse() { }

        public PackageMeta GetPackageMeta()
        {
            return null;
        }

        public BuildConfig GetBuildConfig()
        {
            return null;
        }

        public PublishConfig GetPublishConfig()
        {
            return null;
        }

        public AuthConfig GetAuthConfig()
        {
            return null;
        }
    }
}
