using System.IO.Compression;

namespace ThunderstoreCLI.Game;

public class BepInExManager : ModManager
{
    public static string Identifier => "BepInEx";
    public bool SupportsProfiles(GameDefinition gameDef) => true;
    public void InstallLoader(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest, ZipArchive loaderArchive)
    {
        throw new NotImplementedException();
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
