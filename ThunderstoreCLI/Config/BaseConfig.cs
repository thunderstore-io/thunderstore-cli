namespace ThunderstoreCLI.Config;

class BaseConfig : EmptyConfig
{
    public override PackageConfig GetPackageMeta()
    {
        return new PackageConfig()
        {
            ProjectConfigPath = Defaults.PROJECT_CONFIG_PATH,
            Namespace = "AuthorName",
            Name = "PackageName",
            VersionNumber = "0.0.1",
            Description = "Example mod description",
            WebsiteUrl = "",
            ContainsNsfwContent = false,
            Dependencies = new()
            {
                { "Example-Dependency", "1.0.0" }
            }
        };
    }

    public override InitConfig GetInitConfig()
    {
        return new InitConfig()
        {
            Overwrite = false
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

    public override PublishConfig GetPublishConfig()
    {
        return new PublishConfig()
        {
            File = null,
            Repository = Defaults.REPOSITORY_URL
        };
    }
}
