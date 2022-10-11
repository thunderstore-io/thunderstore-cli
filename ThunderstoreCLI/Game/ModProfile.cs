using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Game;

public class ModProfile : BaseJson<ModProfile>
{
    public string Name { get; set; }
    public string ProfileDirectory { get; set; }
    public Dictionary<string, PackageManifestV1> InstalledModVersions { get; } = new();

#pragma warning disable CS8618
    private ModProfile() { }
#pragma warning restore CS8618

    internal ModProfile(GameDefinition gameDef, string name, string tcliDirectory)
    {
        Name = name;

        var directory = Path.Combine(tcliDirectory, "Profiles", gameDef.Identifier, name);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        ProfileDirectory = directory;
        gameDef.Profiles.Add(this);
    }
}
