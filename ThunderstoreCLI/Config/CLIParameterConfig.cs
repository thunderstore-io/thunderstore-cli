using ThunderstoreCLI.Options;

namespace ThunderstoreCLI.Config;

public abstract class BaseConfig<T> : EmptyConfig where T : BaseOptions
{
    protected T options;

    public BaseConfig(T options)
    {
        this.options = options;
    }

    public override GeneralConfig GetGeneralConfig()
    {
        return new GeneralConfig()
        {
            TcliConfig = options.TcliDirectory
        };
    }
}

public abstract class CLIParameterConfig<T> : BaseConfig<T> where T : PackageOptions
{
    public CLIParameterConfig(T opts) : base(opts) { }

    public override PackageConfig? GetPackageMeta()
    {
        if (options == null)
            return null;
        return new PackageConfig()
        {
            ProjectConfigPath = options.ConfigPath,
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

public class CLIInstallCommandConfig : BaseConfig<InstallOptions>
{
    public CLIInstallCommandConfig(InstallOptions options) : base(options) { }

    public override InstallConfig? GetInstallConfig()
    {
        return new InstallConfig()
        {
            ManagerIdentifier = options.ManagerId
        };
    }
}
