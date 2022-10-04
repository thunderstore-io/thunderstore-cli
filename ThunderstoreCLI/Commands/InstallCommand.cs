using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static class InstallCommand
{
    internal static readonly Dictionary<string, HardcodedGame> IDToHardcoded = new()
    {
        { "ror2", HardcodedGame.ROR2 },
        { "vrising", HardcodedGame.VRISING },
        { "vrising_dedicated", HardcodedGame.VRISING_SERVER }
    };

    internal static readonly Regex FullPackageNameRegex = new(@"^([a-zA-Z0-9_]+)-([a-zA-Z0-9_]+)$");

    public static async Task<int> Run(Config config)
    {
        List<GameDefinition> defs = GameDefinition.GetGameDefinitions(config.GeneralConfig.TcliConfig);
        GameDefinition? def = defs.FirstOrDefault(x => x.Identifier == config.ModManagementConfig.GameIdentifer);
        if (def == null && IDToHardcoded.TryGetValue(config.ModManagementConfig.GameIdentifer!, out var hardcoded))
        {
            def = GameDefinition.FromHardcodedIdentifier(config.GeneralConfig.TcliConfig, hardcoded);
            defs.Add(def);
        }
        else
        {
            Write.ErrorExit($"Not configured for the game: {config.ModManagementConfig.GameIdentifer}");
            return 1;
        }

        ModProfile? profile;
        if (config.ModManagementConfig.Global!.Value)
        {
            profile = def.GlobalProfile;
        }
        else
        {
            profile = def.Profiles.FirstOrDefault(x => x.Name == config.ModManagementConfig.ProfileName);
        }
        profile ??= new ModProfile(def, false, config.ModManagementConfig.ProfileName!, config.GeneralConfig.TcliConfig);

        string package = config.ModManagementConfig.Package!;

        HttpClient http = new();

        int returnCode;
        if (File.Exists(package))
        {
            returnCode = await InstallZip(config, http, def, profile, package, null);
        }
        else if (FullPackageNameRegex.IsMatch(package))
        {
            returnCode = await InstallFromRepository(config, http, def, profile, package);
        }
        else
        {
            throw new CommandFatalException($"Package given does not exist as a zip and is not a valid package identifier (namespace-name): {package}");
        }

        if (returnCode != 0)
            return returnCode;

        GameDefinition.SetGameDefinitions(config.GeneralConfig.TcliConfig, defs);

        return 0;
    }

    private static async Task<int> InstallFromRepository(Config config, HttpClient http, GameDefinition game, ModProfile profile, string packageId)
    {
        var packageParts = packageId.Split('-');
        var packageResponse = await http.SendAsync(config.Api.GetPackageMetadata(packageParts[0], packageParts[1]));
        packageResponse.EnsureSuccessStatusCode();
        var package = (await PackageData.DeserializeAsync(await packageResponse.Content.ReadAsStreamAsync()))!;
        var tempZipPath = await DownloadTemp(http, package);
        var returnCode = await InstallZip(config, http, game, profile, tempZipPath, package.Namespace);
        File.Delete(tempZipPath);
        return returnCode;
    }

    private static async Task<int> InstallZip(Config config, HttpClient http, GameDefinition game, ModProfile profile, string zipPath, string? backupNamespace)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var manifestFile = zip.GetEntry("manifest.json") ?? throw new CommandFatalException("Package zip needs a manifest.json!");
        var manifest = await PackageManifestV1.DeserializeAsync(manifestFile.Open())
            ?? throw new CommandFatalException("Package manifest.json is invalid! Please check against https://thunderstore.io/tools/manifest-v1-validator/");

        var modsToInstall = ModDependencyTree.Generate(config, http, manifest).ToArray();

        var downloadTasks = modsToInstall.Select(mod => DownloadTemp(http, mod)).ToArray();
        var spinner = new ProgressSpinner("mods downloaded", downloadTasks);
        await spinner.Start();

        foreach (var (tempZipPath, package) in downloadTasks.Select(x => x.Result).Zip(modsToInstall))
        {
            int returnCode = RunInstaller(game, profile, tempZipPath, package.Namespace);
            File.Delete(tempZipPath);
            if (returnCode != 0)
                return returnCode;
        }

        return RunInstaller(game, profile, zipPath, backupNamespace);
    }

    private static async Task<string> DownloadTemp(HttpClient http, PackageData package)
    {
        string path = Path.GetTempFileName();
        await using var file = File.OpenWrite(path);
        using var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Get, package.LatestVersion!.DownloadUrl!));
        response.EnsureSuccessStatusCode();
        var zipStream = await response.Content.ReadAsStreamAsync();
        await zipStream.CopyToAsync(file);
        return path;
    }

    private static int RunInstaller(GameDefinition game, ModProfile profile, string zipPath, string? backupNamespace)
    {
        string installerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "tcli-bepinex-installer.exe" : "tcli-bepinex-installer";
        var bepinexInstallerPath = Path.Combine(Path.GetDirectoryName(typeof(InstallCommand).Assembly.Location)!, installerName);

        ProcessStartInfo installerInfo = new(bepinexInstallerPath)
        {
            ArgumentList =
            {
                "install",
                game.InstallDirectory,
                profile.ProfileDirectory,
                zipPath
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (backupNamespace != null)
        {
            installerInfo.ArgumentList.Add("--namespace-backup");
            installerInfo.ArgumentList.Add(backupNamespace);
        }

        var installerProcess = Process.Start(installerInfo)!;
        installerProcess.WaitForExit();

        Write.Light(installerProcess.StandardOutput.ReadToEnd());
        string errors = installerProcess.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(errors))
        {
            Write.Error(errors);
        }

        return installerProcess.ExitCode;
    }
}
