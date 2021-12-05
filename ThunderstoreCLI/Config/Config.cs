using System.Net.Http.Headers;

namespace ThunderstoreCLI.Config;

public class Config
{
    public GeneralConfig GeneralConfig { get; private set; }
    public PackageMeta PackageMeta { get; private set; }
    public InitConfig InitConfig { get; private set; }
    public BuildConfig BuildConfig { get; private set; }
    public PublishConfig PublishConfig { get; private set; }
    public AuthConfig AuthConfig { get; private set; }

    private Config(GeneralConfig generalConfig, PackageMeta packageMeta, InitConfig initConfig, BuildConfig buildConfig, PublishConfig publishConfig, AuthConfig authConfig)
    {
        GeneralConfig = generalConfig;
        PackageMeta = packageMeta;
        InitConfig = initConfig;
        BuildConfig = buildConfig;
        PublishConfig = publishConfig;
        AuthConfig = authConfig;
    }

    public string? GetProjectBasePath()
    {
        return Path.GetDirectoryName(GetProjectConfigPath());
    }

    public string GetProjectRelativePath(string path)
    {
        return Path.GetFullPath(Path.Join(GetProjectBasePath(), path));
    }

    public string GetPackageIconPath()
    {
        if (BuildConfig.IconPath is null)
        {
            throw new Exception("BuildConfig.IconPath can't be null");
        }
        return GetProjectRelativePath(BuildConfig.IconPath);
    }

    public string GetPackageReadmePath()
    {
        if (BuildConfig.ReadmePath is null)
        {
            throw new Exception("BuildConfig.ReadmePath can't be null");
        }
        return GetProjectRelativePath(BuildConfig.ReadmePath);
    }

    public string GetProjectConfigPath()
    {
        if (GeneralConfig.ProjectConfigPath is null)
        {
            throw new Exception("GeneralConfig.ProjectConfigPath can't be null");
        }
        return Path.GetFullPath(GeneralConfig.ProjectConfigPath);
    }

    public string GetBuildOutputDir()
    {
        if (BuildConfig.OutDir is null)
        {
            throw new Exception("BuildConfig.OutDir can't be null");
        }
        return GetProjectRelativePath(BuildConfig.OutDir);
    }

    public string GetPackageId()
    {
        return $"{PackageMeta.Namespace}-{PackageMeta.Name}-{PackageMeta.VersionNumber}";
    }

    public string GetBuildOutputFile()
    {
        return Path.GetFullPath(Path.Join(GetBuildOutputDir(), $"{GetPackageId()}.zip"));
    }

    public string GetRepositoryBaseUrl()
    {
        if (PublishConfig.Repository is null)
        {
            throw new Exception("PublishConfig.Repository can't be null");
        }
        var repo = PublishConfig.Repository.TrimEnd('/');
        return $"{repo}/api/experimental/";
    }

    public string GetPackageSubmitUrl()
    {
        return $"{this.GetRepositoryBaseUrl()}submission/submit/";
    }

    public string GetUserMediaUploadInitiateUrl()
    {
        return $"{this.GetRepositoryBaseUrl()}usermedia/initiate-upload/";
    }

    public string GetUserMediaUploadAbortUrl(string uploadUuid)
    {
        return $"{this.GetRepositoryBaseUrl()}usermedia/{uploadUuid}/abort-upload/";
    }

    public string GetUserMediaUploadFinishUrl(string uploadUuid)
    {
        return $"{this.GetRepositoryBaseUrl()}usermedia/{uploadUuid}/finish-upload/";
    }

    public AuthenticationHeaderValue GetAuthHeader()
    {
        return new AuthenticationHeaderValue("Bearer", AuthConfig.AuthToken);
    }

    public static Config Parse(params IConfigProvider[] configProviders)
    {
        var generalConfig = new GeneralConfig();
        var packageMeta = new PackageMeta();
        var initConfig = new InitConfig();
        var buildConfig = new BuildConfig();
        var publishConfig = new PublishConfig();
        var authConfig = new AuthConfig();
        var result = new Config(generalConfig, packageMeta, initConfig, buildConfig, publishConfig, authConfig);
        foreach (var provider in configProviders)
        {
            provider.Parse(result);
            Merge(generalConfig, provider.GetGeneralConfig(), false);
            Merge(packageMeta, provider.GetPackageMeta(), false);
            Merge(initConfig, provider.GetInitConfig(), false);
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
    public string? ProjectConfigPath { get; set; }
}

public class PackageMeta
{
    public string? Namespace { get; set; }
    public string? Name { get; set; }
    public string? VersionNumber { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool? ContainsNsfwContent { get; set; }
    public Dictionary<string, string>? Dependencies { get; set; }
}

public class InitConfig
{
    public bool? Overwrite { get; set; }

    public bool ShouldOverwrite()
    {
        return Overwrite ?? false;
    }
}

public struct CopyPathMap
{
    public readonly string From;
    public readonly string To;

    public CopyPathMap(string from, string to)
    {
        From = from;
        To = to;
    }
}

public class BuildConfig
{
    public string? IconPath { get; set; }
    public string? ReadmePath { get; set; }
    public string? OutDir { get; set; }
    public List<CopyPathMap>? CopyPaths { get; set; }
}

public class PublishConfig
{
    public string? File { get; set; }
    public string? Repository { get; set; }
    public string[]? Communities { get; set; }
    public string[]? Categories { get; set; }
}

public class AuthConfig
{
    public string? AuthToken { get; set; }
}
