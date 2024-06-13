using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Game;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

public static partial class InstallCommand
{
    // will match either ab-cd or ab-cd-123.456.7890
    internal static readonly Regex FullPackageNameRegex = new Regex(@"^(?<fullname>(?<namespace>[\w-\.]+)-(?<name>\w+))(?:|-(?<version>\d+\.\d+\.\d+))$");

    public static async Task<int> Run(Config config)
    {
        var defCollection = GameDefinitionCollection.FromDirectory(config.GeneralConfig.TcliConfig);
        var defs = defCollection.List;
        GameDefinition? def = defs.FirstOrDefault(x => x.Identifier == config.ModManagementConfig.GameIdentifer);
        if (def == null)
        {
            Write.ErrorExit($"Not configured for the game: {config.ModManagementConfig.GameIdentifer}");
            return 1;
        }

        ModProfile? profile = def.Profiles.FirstOrDefault(x => x.Name == config.ModManagementConfig.ProfileName);
        profile ??= new ModProfile(def, config.ModManagementConfig.ProfileName!, config.GeneralConfig.TcliConfig);

        string package = config.ModManagementConfig.Package!;

        HttpClient http = new();

        int returnCode;
        Match packageMatch = FullPackageNameRegex.Match(package);
        if (File.Exists(package))
        {
            returnCode = await InstallZip(config, http, def, profile, package, null, null, false);
        }
        else if (packageMatch.Success)
        {
            returnCode = await InstallFromRepository(config, http, def, profile, packageMatch);
        }
        else
        {
            throw new CommandFatalException($"Package given does not exist as a zip and is not a valid package identifier (namespace-name): {package}");
        }

        if (returnCode == 0)
            defCollection.Write();

        return returnCode;
    }

    private static async Task<int> InstallFromRepository(Config config, HttpClient http, GameDefinition game, ModProfile profile, Match packageMatch)
    {
        PackageVersionData? versionData = null;
        Write.Light($"Downloading main package: {packageMatch.Groups["fullname"].Value}");

        var ns = packageMatch.Groups["namespace"];
        var name = packageMatch.Groups["name"];
        var version = packageMatch.Groups["version"];
        if (version.Success)
        {
            var versionResponse = await http.SendAsync(config.Api.GetPackageVersionMetadata(ns.Value, name.Value, version.Value));
            if (!versionResponse.IsSuccessStatusCode)
                throw new CommandFatalException($"Couldn't find version {version} of mod {ns}-{name}");
            versionData = (await PackageVersionData.DeserializeAsync(await versionResponse.Content.ReadAsStreamAsync()))!;
        }
        var packageResponse = await http.SendAsync(config.Api.GetPackageMetadata(ns.Value, name.Value));
        if (!packageResponse.IsSuccessStatusCode)
            throw new CommandFatalException($"Could not find package with the name {ns}-{name}");
        var packageData = await PackageData.DeserializeAsync(await packageResponse.Content.ReadAsStreamAsync());

        versionData ??= packageData!.LatestVersion!;

        var zipPath = await config.Cache.GetFileOrDownload($"{versionData.FullName}.zip", versionData.DownloadUrl!);
        var returnCode = await InstallZip(config, http, game, profile, zipPath, versionData.Namespace!, packageData!.CommunityListings!.First().Community, packageData.CommunityListings!.First().Categories!.Contains("Modpacks"));
        return returnCode;
    }

    private static async Task<int> InstallZip(Config config, HttpClient http, GameDefinition game, ModProfile profile, string zipPath, string? backupNamespace, string? sourceCommunity, bool isModpack)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var manifestFile = zip.GetEntry("manifest.json") ?? throw new CommandFatalException("Package zip needs a manifest.json!");
        var manifest = await PackageManifestV1.DeserializeAsync(manifestFile.Open())
            ?? throw new CommandFatalException("Package manifest.json is invalid! Please check against https://thunderstore.io/tools/manifest-v1-validator/");

        manifest.Namespace ??= backupNamespace;

        var dependenciesToInstall = ModDependencyTree.Generate(config, http, manifest, sourceCommunity, isModpack)
            .Where(dependency => !profile.InstalledModVersions.ContainsKey(dependency.FullNameParts["fullname"].Value))
            .ToArray();

        if (dependenciesToInstall.Length > 0)
        {
            var totalSize = dependenciesToInstall
                .Where(d => !config.Cache.ContainsFile($"{d.FullName}-{d.VersionNumber}.zip"))
                .Select(d => d.FileSize)
                .Sum();
            if (totalSize != 0)
            {
                Write.Light($"Total estimated download size: {MiscUtils.GetSizeString(totalSize)}");
            }

            var downloadTasks = dependenciesToInstall.Select(mod =>
                config.Cache.GetFileOrDownload($"{mod.FullName}-{mod.VersionNumber}.zip", mod.DownloadUrl!)
            ).ToArray();

            var spinner = new ProgressSpinner("dependencies downloaded", downloadTasks);
            await spinner.Spin();

            foreach (var (tempZipPath, pVersion) in downloadTasks.Select(x => x.Result).Zip(dependenciesToInstall))
            {
                int returnCode = RunInstaller(game, profile, tempZipPath, pVersion.FullNameParts["namespace"].Value);
                if (returnCode == 0)
                {
                    Write.Success($"Installed mod: {pVersion.FullName}");
                }
                else
                {
                    Write.Error($"Failed to install mod: {pVersion.FullName}");
                    return returnCode;
                }
                profile.InstalledModVersions[pVersion.FullNameParts["fullname"].Value] = new InstalledModVersion(pVersion.FullNameParts["fullname"].Value, pVersion.VersionNumber!, pVersion.Dependencies!);
            }
        }

        var exitCode = RunInstaller(game, profile, zipPath, backupNamespace);
        if (exitCode == 0)
        {
            profile.InstalledModVersions[manifest.FullName] = new InstalledModVersion(manifest.FullName, manifest.VersionNumber!, manifest.Dependencies!);
            Write.Success($"Installed mod: {manifest.FullName}-{manifest.VersionNumber}");
        }
        else
        {
            Write.Error($"Failed to install mod: {manifest.FullName}-{manifest.VersionNumber}");
        }
        return exitCode;
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
            RedirectStandardError = true
        };
        if (backupNamespace != null)
        {
            installerInfo.ArgumentList.Add("--namespace-backup");
            installerInfo.ArgumentList.Add(backupNamespace);
        }

        var installerProcess = Process.Start(installerInfo)!;
        installerProcess.WaitForExit();

        string errors = installerProcess.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(errors))
        {
            Write.Error(errors);
        }

        return installerProcess.ExitCode;
    }
}
