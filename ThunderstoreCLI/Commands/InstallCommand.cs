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
    // TODO: stop hardcoding this, ecosystem-schema (also applies to logic in GameDefintion)
    internal static readonly Dictionary<string, HardcodedGame> IDToHardcoded = new()
    {
        { "ror2", HardcodedGame.ROR2 },
        { "vrising", HardcodedGame.VRISING },
        { "vrising_dedicated", HardcodedGame.VRISING_SERVER }
    };

    // will match either ab-cd or ab-cd-123.456.7890
    internal static readonly Regex FullPackageNameRegex = new(@"^(\w+)-(\w+)(?:|-(\d+\.\d+\.\d+))$");

    public static async Task<int> Run(Config config)
    {
        using var defCollection = GameDefintionCollection.FromDirectory(config.GeneralConfig.TcliConfig);
        var defs = defCollection.List;
        GameDefinition? def = defs.FirstOrDefault(x => x.Identifier == config.ModManagementConfig.GameIdentifer);
        if (def == null && IDToHardcoded.TryGetValue(config.ModManagementConfig.GameIdentifer!, out var hardcoded))
        {
            def = GameDefinition.FromHardcodedIdentifier(config.GeneralConfig.TcliConfig, hardcoded);
            defs.Add(def);
        }
        else if (def == null)
        {
            Write.ErrorExit($"Not configured for the game: {config.ModManagementConfig.GameIdentifer}");
            return 1;
        }

        ModProfile? profile = def.Profiles.FirstOrDefault(x => x.Name == config.ModManagementConfig.ProfileName);
        profile ??= new ModProfile(def, config.ModManagementConfig.ProfileName!, config.GeneralConfig.TcliConfig);

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

        if (returnCode == 0)
            defCollection.Validate();

        return returnCode;
    }

    private static async Task<int> InstallFromRepository(Config config, HttpClient http, GameDefinition game, ModProfile profile, string packageId)
    {
        var packageParts = packageId.Split('-');

        PackageVersionData version;
        if (packageParts.Length == 3)
        {
            var versionResponse = await http.SendAsync(config.Api.GetPackageVersionMetadata(packageParts[0], packageParts[1], packageParts[2]));
            versionResponse.EnsureSuccessStatusCode();
            version = (await PackageVersionData.DeserializeAsync(await versionResponse.Content.ReadAsStreamAsync()))!;
        }
        else
        {
            var packageResponse = await http.SendAsync(config.Api.GetPackageMetadata(packageParts[0], packageParts[1]));
            packageResponse.EnsureSuccessStatusCode();
            version = (await PackageData.DeserializeAsync(await packageResponse.Content.ReadAsStreamAsync()))!.LatestVersion!;
        }


        var tempZipPath = await DownloadTemp(http, version);
        var returnCode = await InstallZip(config, http, game, profile, tempZipPath, version.Namespace!);
        File.Delete(tempZipPath);
        return returnCode;
    }

    private static async Task<int> InstallZip(Config config, HttpClient http, GameDefinition game, ModProfile profile, string zipPath, string? backupNamespace)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var manifestFile = zip.GetEntry("manifest.json") ?? throw new CommandFatalException("Package zip needs a manifest.json!");
        var manifest = await PackageManifestV1.DeserializeAsync(manifestFile.Open())
            ?? throw new CommandFatalException("Package manifest.json is invalid! Please check against https://thunderstore.io/tools/manifest-v1-validator/");

        manifest.Namespace ??= backupNamespace;

        var dependenciesToInstall = ModDependencyTree.Generate(config, http, manifest)
            .Where(dependency => !profile.InstalledModVersions.ContainsKey(dependency.Fullname!))
            .ToArray();

        if (dependenciesToInstall.Length > 0)
        {
            var downloadTasks = dependenciesToInstall.Select(mod => DownloadTemp(http, mod.LatestVersion!)).ToArray();

            var spinner = new ProgressSpinner("mods downloaded", downloadTasks);
            await spinner.Spin();

            foreach (var (tempZipPath, package) in downloadTasks.Select(x => x.Result).Zip(dependenciesToInstall))
            {
                int returnCode = RunInstaller(game, profile, tempZipPath, package.Namespace);
                File.Delete(tempZipPath);
                if (returnCode == 0)
                {
                    Write.Success($"Installed mod: {package.Fullname}-{package.LatestVersion!.VersionNumber}");
                }
                else
                {
                    Write.Error($"Failed to install mod: {package.Fullname}-{package.LatestVersion!.VersionNumber}");
                    return returnCode;
                }
                profile.InstalledModVersions[package.Fullname!] = new PackageManifestV1(package.LatestVersion!);
            }
        }

        var exitCode = RunInstaller(game, profile, zipPath, backupNamespace);
        if (exitCode == 0)
        {
            profile.InstalledModVersions[manifest.FullName] = manifest;
            Write.Success($"Installed mod: {manifest.FullName}-{manifest.VersionNumber}");
        }
        else
        {
            Write.Error($"Failed to install mod: {manifest.FullName}-{manifest.VersionNumber}");
        }
        return exitCode;
    }

    // TODO: replace with a mod cache
    private static async Task<string> DownloadTemp(HttpClient http, PackageVersionData version)
    {
        string path = Path.GetTempFileName();
        await using var file = File.OpenWrite(path);
        using var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Get, version.DownloadUrl!));
        response.EnsureSuccessStatusCode();
        var zipStream = await response.Content.ReadAsStreamAsync();
        await zipStream.CopyToAsync(file);
        return path;
    }

    // TODO: conflict handling
    private static int RunInstaller(GameDefinition game, ModProfile profile, string zipPath, string? backupNamespace)
    {
        // TODO: how to decide which installer to run?
        string installerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "tcli-bepinex-installer.exe" : "tcli-bepinex-installer";
        var bepinexInstallerPath = Path.Combine(AppContext.BaseDirectory, installerName);

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
