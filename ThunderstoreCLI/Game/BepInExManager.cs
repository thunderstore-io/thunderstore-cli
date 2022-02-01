using System.IO.Compression;

namespace ThunderstoreCLI.Game;

public class BepInExManager : ModManager
{
    public static string Identifier => "BepInEx";
    public bool SupportsProfiles(GameDefinition gameDef) => true;
    public void InstallLoader(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest, ZipArchive loaderArchive)
    {
        var winhttp = loaderArchive.Entries.First(x => x.Name == "winhttp.dll");
        var doorstop_config = loaderArchive.Entries.First(x => x.Name == "doorstop_config.ini");

        var gameDir = gameDef.InstallDirectory;

        winhttp.ExtractToFile(Path.Combine(gameDir, "winhttp.dll"), true);
        doorstop_config.ExtractToFile(Path.Combine(gameDir, "doorstop_config.ini"));

        var bepFiles = loaderArchive.Entries
            .Where(x => x != winhttp && x != doorstop_config)
            .Where(x => Path.GetPathRoot(x.FullName)?.StartsWith("BepInExPack") ?? false);

        foreach (var file in bepFiles)
        {
            var actualPath = Path.GetRelativePath(Path.GetPathRoot(file.FullName)!, file.FullName);
            file.ExtractToFile(Path.Combine(profile.ProfileDirectory, actualPath));
        }
    }
    public void UninstallLoader(GameDefinition gameDef, ModProfile profile)
    {
        throw new NotImplementedException();
    }
    public void InstallMod(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest, ZipArchive modArchive)
    {
        throw new NotImplementedException();
    }
    public void UninstallMod(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest)
    {
        throw new NotImplementedException();
    }
    public int RunGame(GameDefinition gameDef, ModProfile profile)
    {
        throw new NotImplementedException();
    }
}
