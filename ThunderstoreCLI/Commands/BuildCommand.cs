using System.IO.Compression;
using System.Text;
using System.Text.Json;

using static Crayon.Output;

namespace ThunderstoreCLI.Commands
{
    public static class BuildCommand
    {
        public class ArchivePlan
        {
            public Config.Config Config { get; protected set; }
            public bool HasWarnings { get; protected set; }
            public bool HasErrors { get; protected set; }

            protected Dictionary<string, Func<byte[]>> plan;
            protected Dictionary<string, string> duplicateMap;
            protected HashSet<string> directories;
            protected HashSet<string> files;

            public ArchivePlan(Config.Config config)
            {
                Config = config;
                plan = new();
                duplicateMap = new();
                directories = new();
                files = new();
            }

            public void AddPlan(string path, Func<byte[]> dataGetter)
            {
                string? key = path.ToLowerInvariant();

                var directoryKeys = new HashSet<string>();
                string? pathParts = key;
                int lastSeparatorIndex = pathParts.LastIndexOf("/");
                while (lastSeparatorIndex > 0)
                {
                    pathParts = pathParts[..lastSeparatorIndex];
                    directoryKeys.Add(pathParts);
                    lastSeparatorIndex = pathParts.LastIndexOf("/");
                }

                if (duplicateMap.ContainsKey(key))
                {
                    string? duplicatePath = duplicateMap[key];
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
                    foreach (string? entry in directoryKeys)
                    {
                        directories.Add(entry);
                    }
                    Write.Light($"Planned for /{path}");
                }
            }

            public Dictionary<string, Func<byte[]>>.Enumerator GetEnumerator() => plan.GetEnumerator();
        }

        public static int Run(Config.Config config)
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

        public static int DoBuild(Config.Config config)
        {
            string? packageId = config.GetPackageId();
            Write.WithNL($"Building {Cyan(packageId)}", after: true);

            string? readmePath = config.GetPackageReadmePath();
            if (!File.Exists(readmePath))
            {
                Write.ErrorExit($"Readme not found from the declared path: {White(Dim(readmePath))}");
                return 1;
            }

            string? iconPath = config.GetPackageIconPath();
            if (!File.Exists(iconPath))
            {
                Write.ErrorExit($"Icon not found from the declared path: {White(Dim(iconPath))}");
                return 1;
            }

            string? outDir = config.GetBuildOutputDir();
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            string? filename = config.GetBuildOutputFile();

            Write.Line($"Output path {Cyan(filename)}");

            bool encounteredIssues = false;

            var plan = new ArchivePlan(config);

            Write.Header("Planning for files to include in build");

            plan.AddPlan("icon.png", () => File.ReadAllBytes(iconPath));
            plan.AddPlan("README.md", () => File.ReadAllBytes(readmePath));
            plan.AddPlan("manifest.json", () => Encoding.UTF8.GetBytes(SerializeManifest(config)));

            if (config.BuildConfig.CopyPaths is not null)
            {
                foreach (Config.CopyPathMap pathMap in config.BuildConfig.CopyPaths)
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

            using (FileStream? outputFile = File.Open(filename, FileMode.Create))
            {
                using var archive = new ZipArchive(outputFile, ZipArchiveMode.Create);
                foreach (KeyValuePair<string, Func<byte[]>> entry in plan)
                {
                    Write.Light($"Writing /{entry.Key}");
                    ZipArchiveEntry? archiveEntry = archive.CreateEntry(entry.Key, CompressionLevel.Optimal);
                    using var writer = new BinaryWriter(archiveEntry.Open());
                    writer.Write(entry.Value());
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
            string? basePath = plan.Config.GetProjectRelativePath(sourcePath);
            if (Directory.Exists(basePath))
            {
                string? destDirectory = FormatArchivePath(destinationPath, false);
                if (!destDirectory.EndsWith('/'))
                {
                    destDirectory = $"{destDirectory}/";
                }

                foreach (string filename in Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories))
                {
                    string? targetPath = FormatArchivePath($"{destDirectory}{filename[(basePath.Length + 1)..]}");
                    plan.AddPlan(targetPath, () => File.ReadAllBytes(filename));
                }
                return true;
            }
            else if (File.Exists(basePath))
            {
                if (destinationPath.EndsWith("/"))
                {
                    string? filename = Path.GetFileName(basePath);
                    string? targetPath = FormatArchivePath($"{destinationPath}{filename}");
                    plan.AddPlan(targetPath, () => File.ReadAllBytes(basePath));
                    return true;
                }
                else
                {
                    string? targetPath = FormatArchivePath(destinationPath);
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
        public static char[] GetInvalidFileNameChars()
        {
            return new char[]
{
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/'
};
        }

        public static string FormatArchivePath(string path, bool validate = true)
        {
            string? result = path.Replace('\\', '/');

            // Strip leading path traversals, since everything has to relate to
            // the root.
            int firstSeparatorIndex = result.IndexOf('/');
            while (firstSeparatorIndex > -1 && string.IsNullOrEmpty(result[..firstSeparatorIndex].Replace(".", "")))
            {
                result = result[(firstSeparatorIndex + 1)..];
                firstSeparatorIndex = result.IndexOf('/');
            }

            result = result.TrimEnd('/');

            // Very rudimentary validation, but it's better than nothing
            if (validate)
            {
                foreach (string? entry in result.Split("/"))
                {
                    if (string.IsNullOrWhiteSpace(entry.Replace(".", "")) || entry.IndexOfAny(GetInvalidFileNameChars()) > -1)
                    {
                        throw new CommandException($"Invalid path defined for a zip entry. Parsed: {result}, Original: {path}");
                    }
                }
            }

            return result;
        }

        public static string SerializeManifest(Config.Config config)
        {
            Dictionary<string, string>? dependencies = config.PackageMeta.Dependencies ?? new Dictionary<string, string>();
            var manifest = new PackageManifestV1()
            {
                Namespace = config.PackageMeta.Namespace,
                Name = config.PackageMeta.Name,
                Description = config.PackageMeta.Description,
                VersionNumber = config.PackageMeta.VersionNumber,
                WebsiteUrl = config.PackageMeta.WebsiteUrl,
                Dependencies = dependencies.Select(x => $"{x.Key}-{x.Value}").ToArray()
            };
            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(manifest, serializerOptions);
        }

        public static List<string> ValidateConfig(Config.Config config, bool throwIfErrors = true)
        {
            var v = new Config.Validator("build");
            v.AddIfEmpty(config.PackageMeta.Namespace, "Package Namespace");
            v.AddIfEmpty(config.PackageMeta.Name, "Package Name");
            v.AddIfNotSemver(config.PackageMeta.VersionNumber, "Package VersionNumber");
            v.AddIfEmpty(config.BuildConfig.OutDir, "Build OutDir");

            if (throwIfErrors)
            {
                v.ThrowIfErrors();
            }

            return v.GetErrors();
        }
    }
}
