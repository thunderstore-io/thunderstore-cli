using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class ListCommand
{
    public static void Run(Config config)
    {
        var collection = GameDefinitionCollection.FromDirectory(config.GeneralConfig.TcliConfig);

        foreach (var game in collection)
        {
            Write.Line($"{game.Identifier}:");
            Write.Line($"  Name: {game.Name}");
            Write.Line($"  Path: {game.InstallDirectory}");
            Write.Line($"  Profiles:");
            foreach (var profile in game.Profiles)
            {
                Write.Line($"    Name: {profile.Name}");
                Write.Line($"    Mods:");
                foreach (var mod in profile.InstalledModVersions.Values)
                {
                    Write.Line($"      {mod.FullName}-{mod.VersionNumber}");
                }
            }
        }
    }
}
