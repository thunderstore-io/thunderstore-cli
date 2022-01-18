using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI.Game;

public class ModProfile : BaseJson<ModProfile>
{
    public bool IsGlobal { get; }
    public string Name { get; }
    public string ProfileDirectory { get; }
    public List<PackageManifestV1> InstalledMods { get; set; } = new();

    internal ModProfile(GameDefinition gameDef, bool global, string name, string tcliDirectory)
    {
        IsGlobal = global;
        Name = name;

        var directory = Path.Combine(tcliDirectory, "Profiles", gameDef.Identifier, name);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        ProfileDirectory = directory;
    }
}
