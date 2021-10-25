namespace ThunderstoreCLI.Config
{
    public interface IConfigProvider
    {
        void Parse(Config currentConfig);

        GeneralConfig? GetGeneralConfig();
        PackageMeta? GetPackageMeta();
        BuildConfig? GetBuildConfig();
        PublishConfig? GetPublishConfig();
        AuthConfig? GetAuthConfig();
    }
}
