using System.Diagnostics.CodeAnalysis;
using ThunderstoreCLI.API;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Configuration;

public class Config
{
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public required GeneralConfig GeneralConfig { get; init; }
    public required PackageConfig PackageConfig { get; init; }
    public required InitConfig InitConfig { get; init; }
    public required BuildConfig BuildConfig { get; init; }
    public required PublishConfig PublishConfig { get; init; }
    public required AuthConfig AuthConfig { get; init; }
    public required ModManagementConfig ModManagementConfig { get; init; }
    public required GameImportConfig GameImportConfig { get; init; }
    public required RunGameConfig RunGameConfig { get; init; }
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local

    private readonly Lazy<ApiHelper> api;
    public ApiHelper Api => api.Value;

    private readonly Lazy<DownloadCache> cache;
    public DownloadCache Cache => cache.Value;

    private Config()
    {
        api = new Lazy<ApiHelper>(() => new ApiHelper(this));
        cache = new Lazy<DownloadCache>(() => new DownloadCache(Path.Combine(GeneralConfig!.TcliConfig, "ModCache")));
    }
    public static Config FromCLI(IConfigProvider cliConfig)
    {
        List<IConfigProvider> providers = new();
        providers.Add(cliConfig);
        providers.Add(new EnvironmentConfig());
        if (cliConfig is CLIConfig)
            providers.Add(new ProjectFileConfig());
        providers.Add(new DefaultConfig());
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
            Categories = PublishConfig.Categories!.GetOrDefault("") ?? Array.Empty<string>(),
            CommunityCategories = PublishConfig.Categories!
                .Where(kvp => kvp.Key != "")
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Communities = PublishConfig.Communities,
            HasNsfwContent = PackageConfig.ContainsNsfwContent ?? false,
            UploadUUID = fileUuid
        };
    }

    public static Config Parse(IConfigProvider[] configProviders)
    {
        Config result = new()
        {
            GeneralConfig = new GeneralConfig(),
            PackageConfig = new PackageConfig(),
            InitConfig = new InitConfig(),
            BuildConfig = new BuildConfig(),
            PublishConfig = new PublishConfig(),
            AuthConfig = new AuthConfig(),
            ModManagementConfig = new ModManagementConfig(),
            GameImportConfig = new GameImportConfig(),
            RunGameConfig = new RunGameConfig(),
        };
        foreach (var provider in configProviders)
        {
            provider.Parse(result);
            Merge(result.GeneralConfig, provider.GetGeneralConfig(), false);
            Merge(result.PackageConfig, provider.GetPackageMeta(), false);
            Merge(result.InitConfig, provider.GetInitConfig(), false);
            Merge(result.BuildConfig, provider.GetBuildConfig(), false);
            Merge(result.PublishConfig, provider.GetPublishConfig(), false);
            Merge(result.AuthConfig, provider.GetAuthConfig(), false);
            Merge(result.ModManagementConfig, provider.GetModManagementConfig(), false);
            Merge(result.GameImportConfig, provider.GetGameImportConfig(), false);
            Merge(result.RunGameConfig, provider.GetRunGameConfig(), false);
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
    public string[]? Communities { get; set; }
    public Dictionary<string, string[]>? Categories { get; set; }
}

public struct InstallerDeclaration
{
    public readonly string Identifier;

    public InstallerDeclaration(string identifier)
    {
        Identifier = identifier;
    }
}

public class InstallConfig
{
    public List<InstallerDeclaration>? InstallerDeclarations { get; set; }
}

public class AuthConfig
{
    public string? AuthToken { get; set; }
}

public class ModManagementConfig
{
    public string? GameIdentifer { get; set; }
    public string? Package { get; set; }
    public string? ProfileName { get; set; }
}

public class GameImportConfig
{
    public string? ExePath { get; set; }
    public string? GameId { get; set; }
}

public class RunGameConfig
{
    public string? GameName { get; set; }
    public string? ProfileName { get; set; }
    public string? UserArguments { get; set; }
}
