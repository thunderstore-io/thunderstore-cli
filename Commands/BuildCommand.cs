using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Crayon.Output;

namespace ThunderstoreCLI.Commands
{
    public static class BuildCommand
    {
        public class ArchivePlan
        {
            public Config.Config Config { get; protected set; }
            public bool HasWarnings { get; protected set; }

            protected Dictionary<string, Func<byte[]>> plan;
            protected Dictionary<string, string> duplicateMap;

            public ArchivePlan(Config.Config config)
            {
                Config = config;
                plan = new();
                duplicateMap = new();
            }

            public void AddPlan(string path, Func<byte[]> dataGetter)
            {
                var key = path.ToLowerInvariant();
                if (duplicateMap.ContainsKey(key))
                {
                    var duplicatePath = duplicateMap[key];
                    if (duplicatePath != path)
                    {
                        Console.WriteLine(Red("ERROR: Case mismatch!"));
                        Console.WriteLine(Red($"ERROR: A file target was added twice to the build with different casing, which is not allowed!"));
                        Console.WriteLine(Red($"ERROR: Previously: {duplicatePath}"));
                        Console.WriteLine(Red($"ERROR: Now: {path}"));
                        throw new CommandException("Duplicate file name in build with mismatching casing.");
                    }
                    Console.WriteLine(Yellow($"WARNING: {path} was added multiple times to the build and will be overwritten"));
                    Console.WriteLine(Yellow($"Re-Planned for /{path}"));
                    plan[path] = dataGetter;
                    HasWarnings = true;
                }
                else
                {
                    plan[path] = dataGetter;
                    duplicateMap[key] = path;
                    Console.WriteLine(Dim($"Planned for /{path}"));
                }
            }

            public Dictionary<string, Func<byte[]>>.Enumerator GetEnumerator()
            {
                return plan.GetEnumerator();
            }
        }

        public static int Run(BuildOptions options, Config.Config config)
        {
            var packageId = $"{config.PackageMeta.Namespace}-{config.PackageMeta.Name}-{config.PackageMeta.VersionNumber}";
            Console.WriteLine($"Building {Cyan(packageId)}");
            Console.WriteLine();

            var readmePath = config.GetPackageReadmePath();
            if (!File.Exists(readmePath))
            {
                Console.WriteLine(Red($"ERROR: Readme not found from the declared path: {readmePath}"));
                Console.WriteLine(Red("Exiting"));
                return 1;
            }

            var iconPath = config.GetPackageIconPath();
            if (!File.Exists(iconPath))
            {
                Console.WriteLine(Red($"ERROR: Icon not found from the declared path: {iconPath}"));
                Console.WriteLine(Red("Exiting"));
                return 1;
            }

            var outDir = config.GetBuildOutputDir();
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            var filename = Path.GetFullPath(Path.Join(outDir, $"{packageId}.zip"));

            Console.WriteLine($"Output path: {Dim(filename)}");
            Console.WriteLine();

            var encounteredIssues = false;

            var plan = new ArchivePlan(config);

            Console.WriteLine("Planning for files to include in build");
            Console.WriteLine(new string('-', 20));

            plan.AddPlan("icon.png", () => File.ReadAllBytes(iconPath));
            plan.AddPlan("README.md", () => File.ReadAllBytes(readmePath));
            plan.AddPlan("manifest.json", () => Encoding.UTF8.GetBytes(SerializeManifest(config)));

            foreach (var pathConfig in config.BuildConfig.CopyPaths)
            {
                encounteredIssues |= !AddPathToArchivePlan(plan, pathConfig.Key, pathConfig.Value);
            }

            Console.WriteLine();
            Console.WriteLine("Writing configured files");
            Console.WriteLine(new string('-', 20));

            using (var outputFile = File.Open(filename, FileMode.Create))
            {
                using (var archive = new ZipArchive(outputFile, ZipArchiveMode.Create))
                {
                    foreach (var entry in plan)
                    {
                        Console.WriteLine(Dim($"Writing /{entry.Key}"));
                        var archiveEntry = archive.CreateEntry(entry.Key, CompressionLevel.Optimal);
                        using (var writer = new StreamWriter(archiveEntry.Open()))
                        {
                            writer.Write(entry.Value());
                        }
                    }
                }
            }

            Console.WriteLine();

            if (encounteredIssues || plan.HasWarnings)
            {
                Console.WriteLine(Yellow("Some issues were encountered when building, see log for more details"));
                return 1;
            }
            else
            {
                Console.WriteLine(Green($"Successfully built {Cyan(packageId)}"));
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
                Console.WriteLine($"WARNING: Nothing found at {sourcePath}, looked from {basePath}");
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

        public static string SerializeManifest(Config.Config config)
        {
            var manifest = new PackageManifestV1()
            {
                Namespace = config.PackageMeta.Namespace,
                Name = config.PackageMeta.Name,
                Description = config.PackageMeta.Description,
                VersionNumber = config.PackageMeta.VersionNumber,
                WebsiteUrl = config.PackageMeta.WebsiteUrl,
                Dependencies = config.PackageMeta.Dependencies.Select(x => $"{x.Key}-{x.Value}").ToArray()
            };
            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(manifest, serializerOptions);
        }
    }
}
