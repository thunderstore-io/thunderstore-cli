using System.Collections;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Game;

public class GameDefinition : BaseJson<GameDefinition>
{
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string InstallDirectory { get; set; }
    public List<ModProfile> Profiles { get; private set; } = new();

#pragma warning disable CS8618
    private GameDefinition() { }
#pragma warning restore CS8618

    internal GameDefinition(string id, string name, string installDirectory, string tcliDirectory)
    {
        Identifier = id;
        Name = name;
        InstallDirectory = installDirectory;
    }

    internal static GameDefinition FromHardcodedIdentifier(string tcliDir, HardcodedGame game)
    {
        return game switch
        {
            HardcodedGame.ROR2 => FromSteamId(tcliDir, 632360, "ror2", "Risk of Rain 2"),
            HardcodedGame.VRISING => FromSteamId(tcliDir, 1604030, "vrising", "V Rising"),
            HardcodedGame.VRISING_SERVER => FromSteamId(tcliDir, 1829350, "vrising_server", "V Rising Dedicated Server"),
            _ => throw new ArgumentException("Invalid enum value", nameof(game))
        };
    }

    internal static GameDefinition FromSteamId(string tcliDir, uint steamId, string id, string name)
    {
        return new GameDefinition(id, name, SteamUtils.FindInstallDirectory(steamId)!, tcliDir);
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

internal enum HardcodedGame
{
    ROR2,
    VRISING,
    VRISING_SERVER
}
