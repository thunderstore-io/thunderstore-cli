namespace ThunderstoreCLI.Configuration;

public interface IConfigProvider
{
    void Parse(Config currentConfig);

    GeneralConfig? GetGeneralConfig();
    PackageConfig? GetPackageMeta();
    InitConfig? GetInitConfig();
    BuildConfig? GetBuildConfig();
    PublishConfig? GetPublishConfig();
    AuthConfig? GetAuthConfig();
    ModManagementConfig? GetInstallConfig();
}
