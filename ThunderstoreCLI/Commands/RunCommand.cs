using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Utils;
using YamlDotNet.Core.Tokens;

namespace ThunderstoreCLI.Commands;

public static class RunCommand
{
    public static int Run(Config config)
    {
        GameDefintionCollection collection = GameDefintionCollection.FromDirectory(config.GeneralConfig.TcliConfig);
        var def = collection.FirstOrDefault(g => g.Identifier == config.RunGameConfig.GameName);

        if (def == null)
        {
            throw new CommandFatalException($"No mods installed for game {config.RunGameConfig.GameName}");
        }

        var profile = def.Profiles.FirstOrDefault(p => p.Name == config.RunGameConfig.ProfileName);

        if (profile == null)
        {
            throw new CommandFatalException($"No profile found with the name {config.RunGameConfig.ProfileName}");
        }

        ProcessStartInfo startInfo = new(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "tcli-bepinex-installer.exe" : "tcli-bepinex-installer")
        {
            ArgumentList =
            {
                "start-instructions",
                def.InstallDirectory,
                profile.ProfileDirectory
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var gameIsProton = SteamUtils.IsProtonGame(def.PlatformId);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.ArgumentList.Add("--game-platform");
            startInfo.ArgumentList.Add(gameIsProton switch
            {
                true => "windows",
                false => "linux"
            });
        }

        var installerProcess = Process.Start(startInfo)!;
        installerProcess.WaitForExit();

        string errors = installerProcess.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(errors) || installerProcess.ExitCode != 0)
        {
            throw new CommandFatalException($"Installer failed with errors:\n{errors}");
        }

        string runArguments = "";
        List<(string key, string value)> runEnvironment = new();
        string[] wineDlls = Array.Empty<string>();

        string[] outputLines = installerProcess.StandardOutput.ReadToEnd().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in outputLines)
        {
            var firstColon = line.IndexOf(':');
            if (firstColon == -1)
            {
                continue;
            }
            var command = line[..firstColon];
            var args = line[(firstColon + 1)..];
            switch (command)
            {
                case "ARGUMENTS":
                    runArguments = args;
                    break;
                case "WINEDLLOVERRIDE":
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        throw new NotSupportedException();
                    }
                    wineDlls = args.Split(':');
                    break;
            }
        }

        var steamDir = SteamUtils.FindSteamDirectory();
        if (steamDir == null)
        {
            throw new CommandFatalException("Couldn't find steam install directory!");
        }
        string steamExeName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            steamExeName = "steam.sh";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            steamExeName = "steam.exe";
        }
        else
        {
            throw new NotImplementedException();
        }

        if (gameIsProton)
        {
            // TODO: force wine DLL overrides with registry
        }

        ProcessStartInfo runSteamInfo = new(Path.Combine(steamDir, steamExeName))
        {
            Arguments = $"-applaunch {def.PlatformId} {runArguments}"
        };

        Write.Note($"Starting appid {def.PlatformId} with arguments: {runArguments}");
        var steamProcess = Process.Start(runSteamInfo)!;
        steamProcess.WaitForExit();
        Write.Success($"Started game with appid {def.PlatformId}");

        return 0;
    }
}
