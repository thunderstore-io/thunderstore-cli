namespace ThunderstoreCLI.Configuration;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
public abstract class EmptyConfig : IConfigProvider
{
    public virtual void Parse(Config currentConfig) { }
    public virtual GeneralConfig? GetGeneralConfig()
    {
        return null;
    }

    public virtual PackageConfig? GetPackageMeta()
    {
        return null;
    }

    public virtual InitConfig? GetInitConfig()
    {
        return null;
    }

    public virtual BuildConfig? GetBuildConfig()
    {
        return null;
    }

    public virtual PublishConfig? GetPublishConfig()
    {
        return null;
    }

    public virtual InstallConfig? GetInstallConfig()
    {
        return null;
    }

    public virtual AuthConfig? GetAuthConfig()
    {
        return null;
    }

    public virtual ModManagementConfig? GetModManagementConfig()
    {
        return null;
    }

    public virtual GameImportConfig? GetGameImportConfig()
    {
        return null;
    }

    public virtual RunGameConfig? GetRunGameConfig()
    {
        return null;
    }
}
