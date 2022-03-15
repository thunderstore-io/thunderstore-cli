using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Game;

public class GameDefinition : BaseJson<GameDefinition>
{
    private const string FILE_NAME = "GameDefintions.json";

    public string Identifier { get; }
    public string Name { get; }
    public string ModManager { get; }
    public string InstallDirectory { get; }
    public List<ModProfile> Profiles { get; private set; } = new();
    public ModProfile GlobalProfile { get; }

    internal GameDefinition(string id, string name, string modManager, string tcliDirectory)
    {
        Identifier = id;
        Name = name;
        ModManager = modManager;
        GlobalProfile = new ModProfile(this, true, "Global", tcliDirectory);
        // TODO: actually find install dir instead of manually setting the path in json
        // yes im lazy
    }

    public static List<GameDefinition> GetGameDefinitions(string tcliDirectory)
    {
        return DeserializeList(File.ReadAllText(Path.Combine(tcliDirectory, FILE_NAME))) ?? new();
    }

    public static void SetGameDefinitions(string tcliDirectory, List<GameDefinition> list)
    {
        File.WriteAllText(Path.Combine(tcliDirectory, FILE_NAME), list.SerializeList(BaseJson.IndentedSettings));
    }
}
