using System.IO.Compression;

namespace ThunderstoreCLI.Game;

public interface ModManager
{
    public static abstract string Identifier { get; }
    public bool SupportsProfiles(GameDefinition gameDef);
    public void InstallLoader(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest, ZipArchive loaderArchive);
    public void UninstallLoader(GameDefinition gameDef, ModProfile profile);
    public void InstallMod(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest, ZipArchive modArchive);
    public void UninstallMod(GameDefinition gameDef, ModProfile profile, PackageManifestV1 manifest);
    public int RunGame(GameDefinition gameDef, ModProfile profile);
}
