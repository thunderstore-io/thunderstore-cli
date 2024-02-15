namespace ThunderstoreCLI.Configuration;

public interface IConfigProvider
{
    void Parse(Config currentConfig);

    GeneralConfig? GetGeneralConfig();
    PackageConfig? GetPackageMeta();
    InitConfig? GetInitConfig();
    BuildConfig? GetBuildConfig();
    PublishConfig? GetPublishConfig();
    InstallConfig? GetInstallConfig();
    AuthConfig? GetAuthConfig();
    ModManagementConfig? GetModManagementConfig();
    GameImportConfig? GetGameImportConfig();
    RunGameConfig? GetRunGameConfig();
}
