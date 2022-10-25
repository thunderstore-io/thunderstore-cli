using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;

namespace ThunderstoreCLI.Models;

public class R2mmGameDescription : BaseYaml<R2mmGameDescription>
{
    public required string uuid { get; set; }
    public required string label { get; set; }
    public required DescriptionMetadata meta { get; set; }
    public required PlatformDistribution[] distributions { get; set; }
    public required object? legacy { get; set; }

    public GameDefinition? ToGameDefintion(Config config)
    {
        var platform = distributions.First(p => p.platform == GamePlatform.steam);
        return GameDefinition.FromPlatformInstall(config.GeneralConfig.TcliConfig, platform.platform, platform.identifier, label, meta.displayName);
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
