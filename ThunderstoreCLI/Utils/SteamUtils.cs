using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ThunderstoreCLI.Utils;

public static class SteamUtils
{
    public static string FindInstallDirectory(uint steamAppId)
    {
        string primarySteamApps = FindSteamAppsDirectory();
        List<string> libraryPaths = new() { primarySteamApps };
        foreach (var file in Directory.EnumerateFiles(primarySteamApps))
        {
            if (!Path.GetFileName(file).Equals("libraryfolders.vdf", StringComparison.OrdinalIgnoreCase))
                continue;
            libraryPaths.AddRange(SteamAppsPathsRegex.Matches(File.ReadAllText(file)).Select(x => x.Groups[1].Value).Select(x => Path.Combine(x, "steamapps")));
            break;
        }

        string acfName = $"appmanifest_{steamAppId}.acf";
        foreach (var library in libraryPaths)
        {
            foreach (var file in Directory.EnumerateFiles(library))
            {
                if (!Path.GetFileName(file).Equals(acfName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var folderName = ManifestInstallLocationRegex.Match(File.ReadAllText(file)).Groups[1].Value;

                return Path.GetFullPath(Path.Combine(library, "common", folderName));
            }
        }
        throw new FileNotFoundException($"Could not find {acfName}, tried the following paths:\n{string.Join('\n', libraryPaths)}");
    }

    private static readonly Regex SteamAppsPathsRegex = new(@"""path""\s+""(.+)""");
    private static readonly Regex ManifestInstallLocationRegex = new(@"""installdir""\s+""(.+)""");

    public static string FindSteamAppsDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return FindSteamAppsDirectoryWin();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return FindSteamAppsDirectoryOsx();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return FindSteamAppsDirectoryLinux();
        else
            throw new NotSupportedException("Unknown operating system");
    }
    private static string FindSteamAppsDirectoryWin()
    {
        throw new NotImplementedException();
    }
    private static string FindSteamAppsDirectoryOsx()
    {
        throw new NotImplementedException();
    }
    private static string FindSteamAppsDirectoryLinux()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] possiblePaths = {
            Path.Combine(homeDir, ".local", "share", "Steam"),
            Path.Combine(homeDir, ".steam", "steam"),
            Path.Combine(homeDir, ".steam", "root"),
            Path.Combine(homeDir, ".steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".steam", "root"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".steam")
        };
        string steamPath = null!;
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                steamPath = path;
                goto FoundSteam;
            }
        }
        throw new DirectoryNotFoundException($"Could not find Steam directory, tried these paths:\n{string.Join('\n', possiblePaths)}");
FoundSteam:

        possiblePaths = new[]
        {
            Path.Combine(steamPath, "steamapps"), // most distros
            Path.Combine(steamPath, "steam", "steamapps"), // ubuntu apparently
            Path.Combine(steamPath, "root", "steamapps"), // no idea
        };
        string steamAppsPath = null!;
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                steamAppsPath = path;
                goto FoundSteamApps;
            }
        }
        throw new DirectoryNotFoundException($"Could not find steamapps directory, tried these paths:\n{string.Join('\n', possiblePaths)}");
FoundSteamApps:

        return steamAppsPath;
    }
}
