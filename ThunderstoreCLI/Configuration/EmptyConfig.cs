namespace ThunderstoreCLI.Configuration;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
public abstract class EmptyConfig : IConfigProvider
{
    public virtual void Parse(Config currentConfig) { }
    public virtual GeneralConfig? GetGeneralConfig() => null;

    public virtual PackageConfig? GetPackageMeta() => null;

    public virtual InitConfig? GetInitConfig() => null;

    public virtual BuildConfig? GetBuildConfig() => null;

    public virtual PublishConfig? GetPublishConfig() => null;

    public virtual InstallConfig? GetInstallConfig() => null;

    public virtual AuthConfig? GetAuthConfig() => null;

    public virtual ModManagementConfig? GetModManagementConfig() => null;

    public virtual GameImportConfig? GetGameImportConfig() => null;

    public virtual RunGameConfig? GetRunGameConfig() => null;
}
