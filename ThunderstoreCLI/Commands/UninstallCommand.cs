using System.Diagnostics;
using System.Runtime.InteropServices;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class UninstallCommand
{
    public static int Run(Config config)
    {
        var defCollection = GameDefinitionCollection.FromDirectory(config.GeneralConfig.TcliConfig);
        GameDefinition? def = defCollection.FirstOrDefault(def => def.Identifier == config.ModManagementConfig.GameIdentifer);
        if (def == null)
        {
            throw new CommandFatalException($"No installed mods for game ${config.ModManagementConfig.GameIdentifer}");
        }
        ModProfile? profile = def.Profiles.FirstOrDefault(p => p.Name == config.ModManagementConfig.ProfileName);
        if (profile == null)
        {
            throw new CommandFatalException($"No profile with the name {config.ModManagementConfig.ProfileName}");
        }

        if (!profile.InstalledModVersions.ContainsKey(config.ModManagementConfig.Package!))
        {
            throw new CommandFatalException($"The package {config.ModManagementConfig.Package} is not installed in the profile {profile.Name}");
        }

        HashSet<string> modsToRemove = new() { config.ModManagementConfig.Package! };
        Queue<string> modsToSearch = new();
        modsToSearch.Enqueue(config.ModManagementConfig.Package!);
        while (modsToSearch.TryDequeue(out var search))
        {
            var searchWithDash = search + '-';
            foreach (var mod in profile.InstalledModVersions.Values)
            {
                if (mod.Dependencies.Any(s => s.StartsWith(searchWithDash)))
                {
                    if (modsToRemove.Add(mod.FullName))
                    {
                        modsToSearch.Enqueue(mod.FullName);
                    }
                }
            }
        }

        foreach (var mod in modsToRemove)
        {
            profile.InstalledModVersions.Remove(mod);
        }

        Write.Line($"The following mods will be uninstalled:\n{string.Join('\n', modsToRemove)}");
        char key;
        do
        {
            Write.NoLine("Continue? (y/n): ");
            key = Console.ReadKey().KeyChar;
            Write.Empty();
        }
        while (key is not 'y' and not 'n');

        if (key == 'n')
            return 0;

        List<string> failedMods = new();
        foreach (var toRemove in modsToRemove)
        {
            string installerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "tcli-bepinex-installer.exe" : "tcli-bepinex-installer";
            var bepinexInstallerPath = Path.Combine(AppContext.BaseDirectory, installerName);

            ProcessStartInfo installerInfo = new(bepinexInstallerPath)
            {
                ArgumentList =
                {
                    "uninstall",
                    def.InstallDirectory,
                    profile.ProfileDirectory,
                    toRemove
                },
                RedirectStandardError = true
            };

            var installerProcess = Process.Start(installerInfo)!;
            installerProcess.WaitForExit();

            Write.Success($"Uninstalled mod: {toRemove}");

            string errors = installerProcess.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(errors) || installerProcess.ExitCode != 0)
            {
                Write.Error(errors);
                failedMods.Add(toRemove);
            }
        }

        if (failedMods.Count != 0)
        {
            throw new CommandFatalException($"The following mods failed to uninstall:\n{string.Join('\n', failedMods)}");
        }

        defCollection.Write();

        return 0;
    }
}
