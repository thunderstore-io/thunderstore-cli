using System;
using System.Collections.Generic;
using System.Text;

namespace ThunderstoreCLI.Config
{
    public interface IConfigProvider
    {
        void Parse();
        PackageMeta GetPackageMeta();
        BuildConfig GetBuildConfig();
        PublishConfig GetPublishConfig();
    }
}
