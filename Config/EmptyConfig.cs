using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThunderstoreCLI.Config
{
    public abstract class EmptyConfig : IConfigProvider
    {
        public virtual void Parse(Config currentConfig) { }
        public virtual GeneralConfig GetGeneralConfig()
        {
            return null;
        }

        public virtual PackageMeta GetPackageMeta()
        {
            return null;
        }

        public virtual BuildConfig GetBuildConfig()
        {
            return null;
        }

        public virtual PublishConfig GetPublishConfig()
        {
            return null;
        }

        public virtual AuthConfig GetAuthConfig()
        {
            return null;
        }
    }
}
