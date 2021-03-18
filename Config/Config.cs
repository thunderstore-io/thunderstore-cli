using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ThunderstoreCLI.Config
{
    public class Config
    {
        public GeneralConfig GeneralConfig { get; private set; }
        public PackageMeta PackageMeta { get; private set; }
        public BuildConfig BuildConfig { get; private set; }
        public PublishConfig PublishConfig { get; private set; }
        public AuthConfig AuthConfig { get; private set; }

        private Config(GeneralConfig generalConfig, PackageMeta packageMeta, BuildConfig buildConfig, PublishConfig publishConfig, AuthConfig authConfig)
        {
            GeneralConfig = generalConfig;
            PackageMeta = packageMeta;
            BuildConfig = buildConfig;
            PublishConfig = publishConfig;
            AuthConfig = authConfig;
        }

        public string GetProjectPath()
        {
            return Path.GetDirectoryName(GetProjectConfigPath());
        }

        public string GetPackageIconPath()
        {
            return Path.GetFullPath(Path.Join(GetProjectPath(), BuildConfig.IconPath));
        }

        public string GetPackageReadmePath()
        {
            return Path.GetFullPath(Path.Join(GetProjectPath(), BuildConfig.ReadmePath));
        }

        public string GetProjectConfigPath()
        {
            return Path.GetFullPath(GeneralConfig.ProjectConfigPath);
        }

        public static Config Parse(params IConfigProvider[] configProviders)
        {
            var generalConfig = new GeneralConfig();
            var packageMeta = new PackageMeta();
            var buildConfig = new BuildConfig();
            var publishConfig = new PublishConfig();
            var authConfig = new AuthConfig();
            var result = new Config(generalConfig, packageMeta, buildConfig, publishConfig, authConfig);
            foreach (var provider in configProviders)
            {
                provider.Parse(result);
                Merge(generalConfig, provider.GetGeneralConfig(), false);
                Merge(packageMeta, provider.GetPackageMeta(), false);
                Merge(buildConfig, provider.GetBuildConfig(), false);
                Merge(publishConfig, provider.GetPublishConfig(), false);
                Merge(authConfig, provider.GetAuthConfig(), false);
            }
            return result;
        }

        public static void Merge<T>(T target, T source, bool overwrite)
        {
            if (source == null)
                return;

            var t = typeof(T);
            var properties = t.GetProperties();

            foreach (var prop in properties)
            {
                var sourceVal = prop.GetValue(source, null);
                if (sourceVal != null)
                {
                    var targetVal = prop.GetValue(target, null);
                    if (targetVal == null || overwrite)
                        prop.SetValue(target, sourceVal, null);
                }
            }
        }
    }

    public class GeneralConfig
    {
        public string ProjectConfigPath { get; set; }
    }

    public class PackageMeta
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string VersionNumber { get; set; }
        public string Description { get; set; }
        public string WebsiteUrl { get; set; }
        public Dictionary<string, string> Dependencies { get; set; }
    }

    public class BuildConfig
    {
        public string IconPath { get; set; }
        public string ReadmePath { get; set; }
        public string OutDir { get; set; }
        public Dictionary<string, string> CopyPaths { get; set; }
    }

    public class PublishConfig
    {
        public string Repository { get; set; }
    }

    public class AuthConfig
    {
        public string DefaultToken { get; set; }
        public Dictionary<string, string> AuthorTokens { get; set; }
    }
}
