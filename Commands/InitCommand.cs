using System;
using System.IO;
using ThunderstoreCLI.Config;

namespace ThunderstoreCLI.Commands
{
    public static class InitCommand
    {
        public static int Run(InitOptions options, Config.Config config)
        {
            var path = config.GetProjectConfigPath();
            var projectDir = Path.GetDirectoryName(path);
            if (projectDir is not null && !Directory.Exists(projectDir))
            {
                Console.WriteLine($"Creating directory {projectDir}");
                Directory.CreateDirectory(projectDir);
            }

            Console.WriteLine($"Creating a new project configuration to {projectDir}");
            if (File.Exists(path) && !options.Overwrite)
            {
                Console.WriteLine($"Project configuration already exists, stopping");
                Console.WriteLine($"Use the --{InitOptions.OVERWRITE_FLAG} to overwrite the file");
                return 1;
            }
            else
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"Project configuration already exists, overwriting");
                }
                ProjectFileConfig.Write(config, path);

                var iconPath = config.GetPackageIconPath();
                if (File.Exists(iconPath))
                {
                    Console.WriteLine("Icon found, skipping creation of default");
                }
                else
                {
                    File.WriteAllBytes(iconPath, Properties.Resources.icon);
                }

                var readmePath = config.GetPackageReadmePath();
                if (File.Exists(readmePath))
                {
                    Console.WriteLine("Readme found, skipping creation of default");
                }
                else
                {
                    File.WriteAllText(readmePath, BuildReadme(config));
                }

                Console.WriteLine("Done!");
                return 0;
            }
        }

        public static string BuildReadme(Config.Config config)
        {
            return $@"
# {config.PackageMeta.Namespace}-{config.PackageMeta.Name}

{config.PackageMeta.Description}
".Trim();
        }
    }
}
