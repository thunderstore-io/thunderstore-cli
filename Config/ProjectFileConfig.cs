using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tommy;

namespace ThunderstoreCLI.Config
{
    class ProjectFileConfig : EmptyConfig
    {
        public static TomlTable Read(Config config)
        {
            var configPath = Paths.GetProjectConfigPath(config);
            if (!File.Exists(configPath))
            {
                throw new CommandException($"Unable to find project configuration file. Looked from {configPath}");
            }
            using (StreamReader reader = new StreamReader(File.OpenRead(configPath)))
            {
                return TOML.Parse(reader);
            }
        }

        public static void Write(Config config, string path)
        {
            var toml = new FormattedTomlTable
            {
                ["config"] =
                {
                    ["schemaVersion"] = "0.0.1"
                },

                ["package"] = new FormattedTomlTable
                {
                    ["namespace"] = config.PackageMeta.Namespace,
                    ["name"] = config.PackageMeta.Name,
                    ["versionNumber"] = config.PackageMeta.VersionNumber,
                    ["description"] = config.PackageMeta.Description,
                    ["websiteUrl"] = config.PackageMeta.WebsiteUrl
                },

                ["package.dependencies"] = TomlUtils.DictToToml(config.PackageMeta.Dependencies),

                ["build"] = new FormattedTomlTable
                {
                    ["icon"] = config.BuildConfig.IconPath,
                    ["readme"] = config.BuildConfig.ReadmePath,
                    ["outdir"] = config.BuildConfig.OutDir
                },

                ["build.copy"] = TomlUtils.DictToToml(config.BuildConfig.CopyPaths),

                ["publish"] = new FormattedTomlTable
                {
                    ["repository"] = config.PublishConfig.Repository
                }
            };
            File.WriteAllText(path, TomlUtils.FormatToml(toml));
        }
    }
}
