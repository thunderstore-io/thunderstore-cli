using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Game;

public class GameDefinition : BaseJson<GameDefinition>
{
    private const string FILE_NAME = "GameDefintions.json";

    public string Identifier { get; }
    public string Name { get; }
    public string InstallDirectory { get; private set; }
    public List<ModProfile> Profiles { get; private set; } = new();
    public ModProfile GlobalProfile { get; }

#pragma warning disable CS8618
    private GameDefinition() { }
#pragma warning restore CS8618

    internal GameDefinition(string id, string name, string installDirectory, string tcliDirectory)
    {
        Identifier = id;
        Name = name;
        InstallDirectory = installDirectory;
        GlobalProfile = new ModProfile(this, true, "Global", tcliDirectory);
    }

    internal static List<GameDefinition> GetGameDefinitions(string tcliDirectory)
    {
        var filename = Path.Combine(tcliDirectory, FILE_NAME);
        if (File.Exists(filename))
            return DeserializeList(File.ReadAllText(filename)) ?? new();
        else
            return new();
    }

    internal static GameDefinition FromHardcodedIdentifier(string tcliDir, HardcodedGame game)
    {
        return game switch
        {
            HardcodedGame.ROR2 => FromSteamId(tcliDir, 632360, "ror2", "Risk of Rain 2"),
            HardcodedGame.VRISING => FromSteamId(tcliDir, 1604030, "vrising", "V Rising"),
            HardcodedGame.VRISING_SERVER => FromSteamId(tcliDir, 1829350, "vrising_server", "V Rising Dedicated Server"),
            HardcodedGame.VRISING_SERVER_BUILTIN => FromSteamId(tcliDir, 1604030, "VRising_Server", "virsing_server_builtin", "V Rising Built-in Server"),
            _ => throw new ArgumentException("Invalid enum value", nameof(game))
        };
    }

    internal static GameDefinition FromSteamId(string tcliDir, uint steamId, string id, string name)
    {
        return new GameDefinition(id, name, SteamUtils.FindInstallDirectory(steamId), tcliDir);
    }

    internal static GameDefinition FromSteamId(string tcliDir, uint steamId, string subdirectory, string id, string name)
    {
        var gameDef = FromSteamId(tcliDir, steamId, id, name);
        gameDef.InstallDirectory = Path.Combine(gameDef.InstallDirectory, subdirectory);
        return gameDef;
    }

    internal static void SetGameDefinitions(string tcliDirectory, List<GameDefinition> list)
    {
        File.WriteAllText(Path.Combine(tcliDirectory, FILE_NAME), list.SerializeList(BaseJson.IndentedSettings));
    }
}

internal enum HardcodedGame
{
    ROR2,
    VRISING,
    VRISING_SERVER,
    VRISING_SERVER_BUILTIN
}
