using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class InstallCommand
{
    private static readonly Dictionary<string, HardcodedGame> IDToHardcoded = new()
    {
        { "ror2", HardcodedGame.ROR2 },
        { "vrising", HardcodedGame.VRISING },
        { "vrising_dedicated", HardcodedGame.VRISING_SERVER },
        { "vrising_builtin", HardcodedGame.VRISING_SERVER_BUILTIN }
    };

    private static readonly Regex FullPackageNameRegex = new(@"^([a-zA-Z0-9_]+)-([a-zA-Z0-9_]+)$");

    public static int Run(Config config)
    {
        List<GameDefinition> defs = GameDefinition.GetGameDefinitions(config.GeneralConfig.TcliConfig);
        GameDefinition? def = defs.FirstOrDefault(x => x.Identifier == config.InstallConfig.GameIdentifer);
        if (def == null && IDToHardcoded.TryGetValue(config.InstallConfig.GameIdentifer!, out var hardcoded))
        {
            def = GameDefinition.FromHardcodedIdentifier(config.GeneralConfig.TcliConfig, hardcoded);
            defs.Add(def);
        }
        else
        {
            Write.ErrorExit($"Not configured for the game: {config.InstallConfig.GameIdentifer}");
            return 1;
        }

        ModProfile? profile;
        if (config.InstallConfig.Global!.Value)
        {
            profile = def.GlobalProfile;
        }
        else
        {
            profile = def.Profiles.FirstOrDefault(x => x.Name == config.InstallConfig.ProfileName);
        }
        profile ??= new ModProfile(def, false, config.InstallConfig.ProfileName!, config.GeneralConfig.TcliConfig);

        string zipPath = config.InstallConfig.Package!;
        bool isTemp = false;
        if (!File.Exists(zipPath))
        {
            var match = FullPackageNameRegex.Match(zipPath);
            if (!match.Success)
            {
                Write.ErrorExit($"Package name does not exist as a file and is not a valid package name (namespace-author): {zipPath}");
            }
            HttpClient http = new();
            var packageResponse = http.Send(config.Api.GetPackageMetadata(match.Groups[1].Value, match.Groups[2].Value));
            using StreamReader responseReader = new(packageResponse.Content.ReadAsStream());
            if (!packageResponse.IsSuccessStatusCode)
            {
                Write.ErrorExit($"Could not find package {zipPath}, got:\n{responseReader.ReadToEnd()}");
                return 1;
            }
            var data = PackageData.Deserialize(responseReader.ReadToEnd());

            zipPath = Path.GetTempFileName();
            isTemp = true;
            using var outFile = File.OpenWrite(zipPath);

            using var downloadStream = http.Send(new HttpRequestMessage(HttpMethod.Get, data!.LatestVersion!.DownloadUrl)).Content.ReadAsStream();

            downloadStream.CopyTo(outFile);
        }

        string installerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "tcli-bepinex-installer.exe" : "tcli-bepinex-installer";
        var bepinexInstallerPath = Path.Combine(Path.GetDirectoryName(typeof(InstallCommand).Assembly.Location)!, installerName);

        ProcessStartInfo installerInfo = new(bepinexInstallerPath)
        {
            ArgumentList =
            {
                "install",
                def.InstallDirectory,
                profile.ProfileDirectory,
                zipPath
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process installerProcess = Process.Start(installerInfo)!;
        installerProcess.WaitForExit();

        Write.Light(installerProcess.StandardOutput.ReadToEnd());
        string errors = installerProcess.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(errors))
        {
            Write.Error(errors);
        }

        if (isTemp)
        {
            File.Delete(zipPath);
        }

        GameDefinition.SetGameDefinitions(config.GeneralConfig.TcliConfig, defs);

        return installerProcess.ExitCode;
    }
}
