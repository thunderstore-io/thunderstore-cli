namespace ThunderstoreCLI.Config
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public abstract class EmptyConfig : IConfigProvider
    {
        public virtual void Parse(Config currentConfig) { }
        public virtual GeneralConfig? GetGeneralConfig() => null;

        public virtual PackageMeta? GetPackageMeta() => null;

        public virtual InitConfig? GetInitConfig() => null;

        public virtual BuildConfig? GetBuildConfig() => null;

        public virtual PublishConfig? GetPublishConfig() => null;

        public virtual AuthConfig? GetAuthConfig() => null;
    }
}
