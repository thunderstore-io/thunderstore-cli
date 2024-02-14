using System.IO.Compression;
using System.Text;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;
using static Crayon.Output;

namespace ThunderstoreCLI.Commands;

public static class BuildCommand
{
    public class ArchivePlan
    {
        public Config Config { get; protected set; }
        public bool HasWarnings { get; protected set; }
        public bool HasErrors { get; protected set; }

        protected Dictionary<string, Func<byte[]>> plan;
        protected Dictionary<string, string> duplicateMap;
        protected HashSet<string> directories;
        protected HashSet<string> files;

        public ArchivePlan(Config config)
        {
            Config = config;
            plan = new();
            duplicateMap = new();
            directories = new();
            files = new();
        }

        public void AddPlan(string path, Func<byte[]> dataGetter)
        {
            var key = path.ToLowerInvariant();

            var directoryKeys = new HashSet<string>();
            var pathParts = key;
            var lastSeparatorIndex = pathParts.LastIndexOf("/");
            while (lastSeparatorIndex > 0)
            {
                pathParts = pathParts.Substring(0, lastSeparatorIndex);
                directoryKeys.Add(pathParts);
                lastSeparatorIndex = pathParts.LastIndexOf("/");
            }

            if (duplicateMap.ContainsKey(key))
            {
                var duplicatePath = duplicateMap[key];
                if (duplicatePath != path)
                {
                    Write.Error(
                        "Case mismatch!",
                        $"A file target was added twice to the build with different casing, which is not allowed!",
                        $"Previously: {White(Dim($"/{duplicatePath}"))}",
                        $"Now: {White(Dim($"/{path}"))}"
                    );
                    HasErrors = true;
                    return;
                }
                Write.Warn(
                    $"{Dim(path)} was added multiple times to the build and will be overwritten",
                    $"Re-Planned for {White(Dim($"/{path}"))}"
                );
                plan[path] = dataGetter;
                HasWarnings = true;
            }
            else if (directories.Contains(key))
            {
                Write.Error(
                    "Filepath conflict!",
                    "A directory already exists in the location where a file was to be placed",
                    $"Path in question: {White(Dim($"/{path}"))}"
                );
                HasErrors = true;
                return;
            }
            else if (directoryKeys.Any(x => files.Contains(x)))
            {
                Write.Error(
                    "Directory path conflict!",
                    "A file already exists in the location where a directory was to be created",
                    $"Path in question: {White(Dim($"/{path}"))}"
                );
                HasErrors = true;
                return;
            }
            else
            {
                plan[path] = dataGetter;
                duplicateMap[key] = path;

                files.Add(key);
                foreach (var entry in directoryKeys)
                {
                    directories.Add(entry);
                }
                Write.Light($"Planned for /{path}");
            }
        }

        public Dictionary<string, Func<byte[]>>.Enumerator GetEnumerator()
        {
            return plan.GetEnumerator();
        }
    }

    public static int Run(Config config)
    {
        try
        {
            ValidateConfig(config);
        }
        catch (CommandException)
        {
            return 1;
        }

        return DoBuild(config);
    }

    public static int DoBuild(Config config)
    {
        var packageId = config.GetPackageId();
        Write.WithNL($"Building {Cyan(packageId)}", after: true);

        var readmePath = config.GetPackageReadmePath();
        if (!File.Exists(readmePath))
        {
            Write.ErrorExit($"Readme not found from the declared path: {White(Dim(readmePath))}");
            return 1;
        }

        var iconPath = config.GetPackageIconPath();
        if (!File.Exists(iconPath))
        {
            Write.ErrorExit($"Icon not found from the declared path: {White(Dim(iconPath))}");
            return 1;
        }

        var outDir = config.GetBuildOutputDir();
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        var filename = config.GetBuildOutputFile();

        Write.Line($"Output path {Cyan(filename)}");

        var encounteredIssues = false;

        var plan = new ArchivePlan(config);

        Write.Header("Planning for files to include in build");

        plan.AddPlan("icon.png", () => File.ReadAllBytes(iconPath));
        plan.AddPlan("README.md", () => File.ReadAllBytes(readmePath));
        plan.AddPlan("manifest.json", () => Encoding.UTF8.GetBytes(SerializeManifest(config)));

        if (config.BuildConfig.CopyPaths is not null)
        {
            foreach (var pathMap in config.BuildConfig.CopyPaths)
            {
                Write.WithNL($"Mapping {Dim(pathMap.From)} to {Dim($"/{pathMap.To}")}", before: true);
                encounteredIssues |= !AddPathToArchivePlan(plan, pathMap.From, pathMap.To);
            }
        }

        if (plan.HasErrors)
        {
            Write.Empty();
            Write.ErrorExit(
                "Build was aborted due to errors identified in planning phase",
                "Adjust your configuration so no issues are present"
            );
            return 1;
        }

        Write.Header("Writing configured files");

        using (var outputFile = File.Open(filename, FileMode.Create))
        {
            using (var archive = new ZipArchive(outputFile, ZipArchiveMode.Create))
            {
                var isWindows = OperatingSystem.IsWindows();
                foreach (var entry in plan)
                {
                    Write.Light($"Writing /{entry.Key}");
                    var archiveEntry = archive.CreateEntry(entry.Key, CompressionLevel.Optimal);
                    if (!isWindows)
                    {
                        // https://github.com/dotnet/runtime/issues/17912#issuecomment-641594638
                        // modifed solution to use a constant instead of a string conversion
                        archiveEntry.ExternalAttributes |= 0b110110100 << 16; // rw-rw-r-- permissions
                    }
                    using (var writer = new BinaryWriter(archiveEntry.Open()))
                    {
                        writer.Write(entry.Value());
                    }
                }
            }
        }

        Write.Empty();

        if (encounteredIssues || plan.HasWarnings)
        {
            Write.Note("Some issues were encountered when building, see output for more details");
            return 1;
        }
        else
        {
            Write.Success($"Successfully built {Cyan(packageId)}");
            return 0;
        }
    }

    public static bool AddPathToArchivePlan(ArchivePlan plan, string sourcePath, string destinationPath)
    {
        var basePath = plan.Config.GetProjectRelativePath(sourcePath);
        if (Directory.Exists(basePath))
        {
            var destDirectory = FormatArchivePath(destinationPath, false);
            if (!destDirectory.EndsWith('/'))
                destDirectory = $"{destDirectory}/";

            foreach (string filename in Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories))
            {
                var targetPath = FormatArchivePath($"{destDirectory}{filename[(basePath.Length + 1)..]}");
                plan.AddPlan(targetPath, () => File.ReadAllBytes(filename));
            }
            return true;
        }
        else if (File.Exists(basePath))
        {
            if (destinationPath.EndsWith("/"))
            {
                var filename = Path.GetFileName(basePath);
                var targetPath = FormatArchivePath($"{destinationPath}{filename}");
                plan.AddPlan(targetPath, () => File.ReadAllBytes(basePath));
                return true;
            }
            else
            {
                var targetPath = FormatArchivePath(destinationPath);
                plan.AddPlan(targetPath, () => File.ReadAllBytes(basePath));
                return true;
            }
        }
        else
        {
            Write.Warn($"Nothing found at {sourcePath}, looked from {basePath}");
            return false;
        }
    }

    // For crossplatform compat, Windows is more restrictive
    public static char[] GetInvalidFileNameChars() => new char[]
    {
        '\"', '<', '>', '|', '\0',
        (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31, ':', '*', '?', '\\', '/'
    };

    public static string FormatArchivePath(string path, bool validate = true)
    {
        var result = path.Replace('\\', '/');

        // Strip leading path traversals, since everything has to relate to
        // the root.
        var firstSeparatorIndex = result.IndexOf('/');
        while (firstSeparatorIndex > -1 && string.IsNullOrEmpty(result.Substring(0, firstSeparatorIndex).Replace(".", "")))
        {
            result = result[(firstSeparatorIndex + 1)..];
            firstSeparatorIndex = result.IndexOf('/');
        }

        result = result.TrimEnd('/');

        // Very rudimentary validation, but it's better than nothing
        if (validate)
        {
            foreach (var entry in result.Split("/"))
            {
                if (string.IsNullOrWhiteSpace(entry.Replace(".", "")) || entry.IndexOfAny(GetInvalidFileNameChars()) > -1)
                    throw new CommandException($"Invalid path defined for a zip entry. Parsed: {result}, Original: {path}");
            }
        }

        return result;
    }

    public static string SerializeManifest(Config config)
    {
        var dependencies = config.PackageConfig.Dependencies ?? new Dictionary<string, string>();
        IEnumerable<InstallerDeclaration>? installerDeclarations = config.InstallConfig.InstallerDeclarations;
        installerDeclarations ??= Array.Empty<InstallerDeclaration>();
        var manifest = new PackageManifestV1()
        {
            Namespace = config.PackageConfig.Namespace,
            Name = config.PackageConfig.Name,
            Description = config.PackageConfig.Description,
            VersionNumber = config.PackageConfig.VersionNumber,
            WebsiteUrl = config.PackageConfig.WebsiteUrl,
            Dependencies = dependencies.Select(x => $"{x.Key}-{x.Value}").ToArray(),
            Installers = installerDeclarations
                .Select(x => new PackageManifestV1.InstallerDeclaration { Identifier = x.Identifier })
                .ToArray()
        };

        return manifest.Serialize(BaseJson.IndentedSettings);
    }

    public static List<string> ValidateConfig(Config config, bool throwIfErrors = true)
    {
        var v = new CommandValidator("build");
        v.AddIfEmpty(config.PackageConfig.Namespace, "Package Namespace");
        v.AddIfEmpty(config.PackageConfig.Name, "Package Name");
        v.AddIfNotSemver(config.PackageConfig.VersionNumber, "Package VersionNumber");
        v.AddIfEmpty(config.BuildConfig.OutDir, "Build OutDir");

        if (throwIfErrors)
        {
            v.ThrowIfErrors();
        }

        return v.GetErrors();
    }
}
