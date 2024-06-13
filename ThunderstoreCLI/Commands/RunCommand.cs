using System.Diagnostics;
using System.Runtime.InteropServices;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class RunCommand
{
    public static int Run(Config config)
    {
        var collection = GameDefinitionCollection.FromDirectory(config.GeneralConfig.TcliConfig);
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

        var isSteam = def.Platform == GamePlatform.Steam;

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

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // TODO: Wine without Steam
        var gameIsProton = isSteam && SteamUtils.IsProtonGame(def.PlatformId!);

        startInfo.ArgumentList.Add("--game-platform");
        startInfo.ArgumentList.Add((isWindows, gameIsProton) switch
        {
            (true, _) => "windows",
            (false, true) => "proton",
            (false, false) => "linux"
        });

        var installerProcess = Process.Start(startInfo)!;
        installerProcess.WaitForExit();

        string errors = installerProcess.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(errors) || installerProcess.ExitCode != 0)
        {
            throw new CommandFatalException($"Installer failed with errors:\n{errors}");
        }

        string runArguments = "";
        string[] wineDlls = Array.Empty<string>();
        List<KeyValuePair<string, string>> environ = new();

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
                        break;
                    }
                    wineDlls = args.Split(':');
                    break;
                case "ENVIRONMENT":
                    var parts = args.Split('=');
                    environ.Add(new(parts[0], parts[1]));
                    break;
            }
        }

        var allArgs = string.Join(' ', runArguments, config.RunGameConfig.UserArguments);

        if (isSteam)
        {
            var steamExePath = SteamUtils.FindSteamExecutable();
            if (steamExePath == null)
            {
                throw new CommandFatalException("Couldn't find steam install directory!");
            }

            if (gameIsProton && wineDlls.Length > 0)
            {
                if (!SteamUtils.ForceLoadProton(def.PlatformId!, wineDlls))
                {
                    throw new CommandFatalException($"No compat files could be found for app id {def.PlatformId}, please run the game at least once.");
                }
            }

            ProcessStartInfo runSteamInfo = new(steamExePath)
            {
                Arguments = $"-applaunch {def.PlatformId} {allArgs}"
            };

            Write.Note($"Starting appid {def.PlatformId} with arguments: {allArgs}");
            var steamProcess = Process.Start(runSteamInfo)!;
            steamProcess.WaitForExit();
            Write.Success($"Started game with appid {def.PlatformId}");
        }
        else if (def.Platform == GamePlatform.Other)
        {
            var exePath = def.ExePath!;

            if (!File.Exists(exePath))
            {
                throw new CommandFatalException($"Executable {exePath} could not be found.");
            }

            ProcessStartInfo process = new(exePath)
            {
                Arguments = allArgs,
                WorkingDirectory = def.InstallDirectory,
            };
            foreach (var (key, val) in environ)
            {
                Write.Line($"{key}: {val}");
                process.Environment.Add(key, val);
            }

            Write.Note($"Starting {exePath} with arguments: {allArgs}");

            Process.Start(process)!.WaitForExit();
        }

        return 0;
    }
}
