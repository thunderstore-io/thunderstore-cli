using System;
using System.Collections.Generic;
using System.Text;

namespace ThunderstoreCLI.Config
{
    class BaseConfig : EmptyConfig
    {
        public override GeneralConfig GetGeneralConfig()
        {
            return new GeneralConfig()
            {
                ProjectConfigPath = "./thunderstore.toml"
            };
        }
        public override BuildConfig GetBuildConfig()
        {
            return new BuildConfig()
            {
                IconPath = "./icon.png",
                ReadmePath = "./README.md",
                OutDir = "./build",
                CopyPaths = new()
                {
                    { new("./dist", "") }
                }
            };
        }

        public override PackageMeta GetPackageMeta()
        {
            return new PackageMeta()
            {
                Namespace = "AuthorName",
                Name = "PackageName",
                VersionNumber = "0.0.1",
                Description = "Example mod description",
                WebsiteUrl = "",
                Dependencies = new()
                {
                    { "Example-Dependency", "1.0.0" }
                }
            };
        }

        public override PublishConfig GetPublishConfig()
        {
            return new PublishConfig()
            {
                Repository = "https://thunderstore.io"
            };
        }
    }
}
