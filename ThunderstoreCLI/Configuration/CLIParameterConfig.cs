using ThunderstoreCLI.Options;

namespace ThunderstoreCLI.Configuration;

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
            TcliConfig = options.TcliDirectory,
            Repository = options.Repository
        };
    }
}

internal interface CLIConfig { }

public abstract class CLIParameterConfig<T> : BaseConfig<T>, CLIConfig where T : PackageOptions
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
            File = options.File
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

public class ModManagementCommandConfig : BaseConfig<ModManagementOptions>
{
    public ModManagementCommandConfig(ModManagementOptions options) : base(options) { }

    public override ModManagementConfig? GetInstallConfig()
    {
        return new ModManagementConfig()
        {
            GameIdentifer = options.GameName,
            ProfileName = options.Profile,
            Package = options.Package
        };
    }
}
