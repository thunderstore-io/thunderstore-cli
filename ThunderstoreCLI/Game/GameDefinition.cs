using System.Collections;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Game;

public class GameDefinition : BaseJson<GameDefinition>
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string InstallDirectory { get; set; }
    public GamePlatform Platform { get; set; }
    public string PlatformId { get; set; }
    public List<ModProfile> Profiles { get; private set; } = new();

#pragma warning disable CS8618
    private GameDefinition() { }
#pragma warning restore CS8618

    internal GameDefinition(string id, string name, string installDirectory, GamePlatform platform, string platformId, string tcliDirectory)
    {
        Identifier = id;
        Name = name;
        InstallDirectory = installDirectory;
        Platform = platform;
        PlatformId = platformId;
    }

    internal static GameDefinition? FromPlatformInstall(string tcliDir, GamePlatform platform, string platformId, string id, string name)
    {
        var gameDir = platform switch
        {
            GamePlatform.steam => SteamUtils.FindInstallDirectory(platformId),
            _ => null
        };
        if (gameDir == null)
        {
            return null;
        }
        return new GameDefinition(id, name, gameDir, platform, platformId, tcliDir);
    }
}

public sealed class GameDefintionCollection : IEnumerable<GameDefinition>, IDisposable
{
    private const string FILE_NAME = "GameDefintions.json";

    private readonly string tcliDirectory;
    private bool shouldWrite = true;
    public List<GameDefinition> List { get; }

    internal static GameDefintionCollection FromDirectory(string tcliDirectory) => new(tcliDirectory);

    private GameDefintionCollection(string tcliDir)
    {
        tcliDirectory = tcliDir;
        var filename = Path.Combine(tcliDirectory, FILE_NAME);
        if (File.Exists(filename))
            List = GameDefinition.DeserializeList(File.ReadAllText(filename)) ?? new();
        else
            List = new();
    }

    public void Validate() => shouldWrite = true;

    public IEnumerator<GameDefinition> GetEnumerator() => List.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

    public void Dispose()
    {
        if (!shouldWrite)
            return;
        File.WriteAllText(Path.Combine(tcliDirectory, FILE_NAME), List.SerializeList(BaseJson.IndentedSettings));
        shouldWrite = false;
    }
}

public enum GamePlatform
{
    steam,
    egs,
    other
}
