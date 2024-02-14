using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;

namespace ThunderstoreCLI.Models;

public class SchemaResponse : BaseJson<SchemaResponse>
{
    public required Dictionary<string, SchemaGame> games { get; set; }
}

public class SchemaGame
{
    public required string uuid { get; set; }
    public required string label { get; set; }
    public required DescriptionMetadata meta { get; set; }
    public required PlatformDistribution[] distributions { get; set; }
    public R2modmanInfo? r2modman { get; set; }

    public GameDefinition? ToGameDefintion(Config config)
    {
        var isServer = r2modman?.gameInstanceType == "server";
        var allowedDirectExe = distributions.Any(d => d.platform == GamePlatform.Other) || isServer;
        if (allowedDirectExe && config.GameImportConfig.ExePath != null)
        {
            return GameDefinition.FromNativeInstall(config, label, meta.displayName);
        }
        var platform = distributions.First(p => p.platform == GamePlatform.Steam);
        return GameDefinition.FromPlatformInstall(config, platform.platform, platform.identifier, label, meta.displayName);
    }
}

public class PlatformDistribution
{
    public required GamePlatform platform { get; set; }
    public required string identifier { get; set; }
}

public class DescriptionMetadata
{
    public required string displayName { get; set; }
    public required string iconUrl { get; set; }
}

public class R2modmanInfo
{
    public required string gameInstanceType { get; set; }
}
