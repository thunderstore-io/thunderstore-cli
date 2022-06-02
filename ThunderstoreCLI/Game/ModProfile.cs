using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Game;

public class ModProfile : BaseJson<ModProfile>
{
    public bool IsGlobal { get; }
    public string Name { get; }
    public string ProfileDirectory { get; }

#pragma warning disable CS8618
    private ModProfile() { }
#pragma warning restore CS8618

    internal ModProfile(GameDefinition gameDef, bool global, string name, string tcliDirectory)
    {
        IsGlobal = global;
        Name = name;

        if (IsGlobal)
        {
            ProfileDirectory = gameDef.InstallDirectory;
            return;
        }

        var directory = Path.Combine(tcliDirectory, "Profiles", gameDef.Identifier, name);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        ProfileDirectory = directory;
        gameDef.Profiles.Add(this);
    }
}
