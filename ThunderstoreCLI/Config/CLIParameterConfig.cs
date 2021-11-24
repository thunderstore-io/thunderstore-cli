using ThunderstoreCLI.Options;

namespace ThunderstoreCLI.Config;

public abstract class CLIParameterConfig<T> : EmptyConfig where T : PackageOptions
{
    protected T options;

    public CLIParameterConfig(T options)
    {
        this.options = options;
    }

    public override GeneralConfig GetGeneralConfig()
    {
        return new GeneralConfig()
        {
            ProjectConfigPath = options.ConfigPath
        };
    }

    public override PackageMeta? GetPackageMeta()
    {
        if (options == null)
            return null;
        return new PackageMeta()
        {
            Namespace = options.Namespace,
            Name = options.Name,
            VersionNumber = options.VersionNumber
        };
    }
}

public class CLIInitCommandConfig : CLIParameterConfig<InitOptions>
{
    public CLIInitCommandConfig(InitOptions options) : base(options) { }

    public override InitConfig GetInitConfig()
    {
        return new InitConfig()
        {
            Overwrite = options.Overwrite
        };
    }
}

public class CLIBuildCommandConfig : CLIParameterConfig<BuildOptions>
{
    public CLIBuildCommandConfig(BuildOptions options) : base(options) { }
}

public class CLIPublishCommandConfig : CLIParameterConfig<PublishOptions>
{
    public CLIPublishCommandConfig(PublishOptions options) : base(options) { }

    public override PublishConfig GetPublishConfig()
    {
        return new PublishConfig()
        {
            File = options.File,
            Repository = options.Repository
        };
    }

    public override AuthConfig GetAuthConfig()
    {
        return new AuthConfig()
        {
            AuthToken = options.Token
        };
    }
}
