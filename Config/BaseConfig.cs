using System;
using System.Collections.Generic;
using System.Text;

namespace ThunderstoreCLI.Config
{
    class BaseConfig : IConfigProvider
    {
        public void Parse() { }

        public BuildConfig GetBuildConfig()
        {
            return new BuildConfig()
            {
                IconPath = "./icon.png",
                ReadmePath = "./README.md",
                OutDir = "./build",
                CopyPaths = new()
                {
                    { "./dist", "./" }
                }
            };
        }

        public PackageMeta GetPackageMeta()
        {
            return new PackageMeta()
            {
                Author = "AuthorName",
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

        public PublishConfig GetPublishConfig()
        {
            return new PublishConfig()
            {
                AuthToken = null,
                Repository = "https://thunderstore.io"
            };
        }
    }
}
