using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Game;

public class GameDefinition : BaseJson<GameDefinition>
{
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
}
