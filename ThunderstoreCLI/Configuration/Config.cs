using System.Diagnostics.CodeAnalysis;
using ThunderstoreCLI.API;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Configuration;

public class Config
{
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public GeneralConfig GeneralConfig { get; private set; }
    public PackageConfig PackageConfig { get; private set; }
    public InitConfig InitConfig { get; private set; }
    public BuildConfig BuildConfig { get; private set; }
    public PublishConfig PublishConfig { get; private set; }
    public AuthConfig AuthConfig { get; private set; }
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local

    private readonly Lazy<ApiHelper> api;
    public ApiHelper Api => api.Value;

    private Config(GeneralConfig generalConfig, PackageConfig packageConfig, InitConfig initConfig, BuildConfig buildConfig, PublishConfig publishConfig, AuthConfig authConfig)
    {
        api = new Lazy<ApiHelper>(() => new ApiHelper(this));
        GeneralConfig = generalConfig;
        PackageConfig = packageConfig;
        InitConfig = initConfig;
        BuildConfig = buildConfig;
        PublishConfig = publishConfig;
        AuthConfig = authConfig;
    }
    public static Config FromCLI(IConfigProvider cliConfig)
    {
        List<IConfigProvider> providers = new();
        providers.Add(cliConfig);
        providers.Add(new EnvironmentConfig());
        if (cliConfig is CLIConfig)
            providers.Add(new ProjectFileConfig());
        providers.Add(new BaseConfig());
        return Parse(providers.ToArray());
    }

    public string? GetProjectBasePath()
    {
        return Path.GetDirectoryName(GetProjectConfigPath());
    }

    public string GetProjectRelativePath(string path)
    {
        return Path.Join(GetProjectBasePath(), path);
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
        if (PackageConfig.ProjectConfigPath is null)
        {
            throw new Exception("GeneralConfig.ProjectConfigPath can't be null");
        }
        return Path.GetFullPath(PackageConfig.ProjectConfigPath);
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
        return $"{PackageConfig.Namespace}-{PackageConfig.Name}-{PackageConfig.VersionNumber}";
    }

    public string GetBuildOutputFile()
    {
        return Path.GetFullPath(Path.Join(GetBuildOutputDir(), $"{GetPackageId()}.zip"));
    }

    public PackageUploadMetadata GetUploadMetadata(string fileUuid)
    {
        return new PackageUploadMetadata()
        {
            AuthorName = PackageConfig.Namespace,
            Categories = PublishConfig.Categories,
            Communities = PublishConfig.Communities,
            HasNsfwContent = PackageConfig.ContainsNsfwContent ?? false,
            UploadUUID = fileUuid
        };
    }

    public static Config Parse(IConfigProvider[] configProviders)
    {
        var generalConfig = new GeneralConfig();
        var packageMeta = new PackageConfig();
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

    public static void Merge<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T target, T source, bool overwrite)
    {
        if (source == null)
            return;

        var t = typeof(T);
        var properties = t.GetProperties();

        foreach (var prop in properties.Where(x => x.SetMethod is not null))
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
    public string TcliConfig { get; set; } = null!;
    public string Repository { get; set; } = null!;
}

public class PackageConfig
{
    public string? ProjectConfigPath { get; set; }
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
