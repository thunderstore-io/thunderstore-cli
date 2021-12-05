using Tommy;
using static Crayon.Output;

namespace ThunderstoreCLI.Config;

class ProjectFileConfig : EmptyConfig
{

    private PackageMeta? PackageMeta { get; set; }

    private BuildConfig? BuildConfig { get; set; }

    private PublishConfig? PublishConfig { get; set; }

    public override void Parse(Config currentConfig)
    {
        var tomlData = Read(currentConfig);
        if (tomlData == null)
            return;

        if (!tomlData.HasKey("config") || !tomlData["config"].HasKey("schemaVersion"))
        {
            ThunderstoreCLI.Write.Warn(
                "Project configuration is lacking schema version",
                "Might not be able to parse configuration as expected"
            );
        }
        if (tomlData["config"]["schemaVersion"] != "0.0.1")
        {
            ThunderstoreCLI.Write.Warn(
                "Unknown project configuration schema version",
                "Might not be able to parse configuration as expected"
            );
        }

        PackageMeta = ParsePackageMeta(tomlData);
        BuildConfig = ParseBuildConfig(tomlData);
        PublishConfig = ParsePublishConfig(tomlData);
    }

    protected static PackageMeta? ParsePackageMeta(TomlTable tomlData)
    {
        if (!tomlData.HasKey("package"))
            return null;

        var packageMeta = tomlData["package"];

        // TODO: Add warnings on missing values
        var result = new PackageMeta()
        {
            Namespace = TomlUtils.SafegetString(packageMeta, "namespace"),
            Name = TomlUtils.SafegetString(packageMeta, "name"),
            VersionNumber = TomlUtils.SafegetString(packageMeta, "versionNumber"),
            Description = TomlUtils.SafegetString(packageMeta, "description"),
            WebsiteUrl = TomlUtils.SafegetString(packageMeta, "websiteUrl"),
            ContainsNsfwContent = TomlUtils.SafegetBool(packageMeta, "containsNsfwContent"),
            Dependencies = new()
        };

        if (packageMeta.HasKey("dependencies"))
        {
            var packageDependencies = packageMeta["dependencies"];
            foreach (var packageName in packageDependencies.Keys)
            {
                // TODO: Validate both are strings if needed?
                result.Dependencies[packageName] = packageDependencies[packageName];
            }
        }

        return result;
    }

    protected static BuildConfig? ParseBuildConfig(TomlTable tomlData)
    {
        if (!tomlData.HasKey("build"))
            return null;

        var buildConfig = tomlData["build"];

        var result = new BuildConfig
        {
            IconPath = TomlUtils.SafegetString(buildConfig, "icon"),
            ReadmePath = TomlUtils.SafegetString(buildConfig, "readme"),
            OutDir = TomlUtils.SafegetString(buildConfig, "outdir"),
            CopyPaths = new()
        };

        if (buildConfig.HasKey("copy"))
        {
            var pathSets = buildConfig["copy"];
            foreach (var entry in pathSets)
            {
                if (!(entry is TomlNode))
                {
                    ThunderstoreCLI.Write.Warn($"Unable to properly parse build config: {entry}", "Skipping entry");
                    continue;
                }

                var node = (TomlNode) entry;
                if (!node.HasKey("source") || !node.HasKey("target"))
                {
                    ThunderstoreCLI.Write.Warn(
                        $"Build config instruction is missing parameters: {node}",
                        "Make sure both 'source' and 'target' are defined",
                        "Skipping entry"
                    );
                    continue;
                }

                result.CopyPaths.Add(new CopyPathMap(node["source"], node["target"]));
            }
        }
        return result;
    }

    protected static PublishConfig? ParsePublishConfig(TomlTable tomlData)
    {
        if (!tomlData.HasKey("publish"))
            return null;

        var publishConfig = tomlData["publish"];

        return new PublishConfig
        {
            Repository = TomlUtils.SafegetString(publishConfig, "repository"),
            Communities = TomlUtils.SafegetStringArray(publishConfig, "communities", Array.Empty<string>()),
            Categories = TomlUtils.SafegetStringArray(publishConfig, "categories", Array.Empty<string>())
        };
    }

    public override PackageMeta? GetPackageMeta()
    {
        return PackageMeta;
    }

    public override BuildConfig? GetBuildConfig()
    {
        return BuildConfig;
    }

    public override PublishConfig? GetPublishConfig()
    {
        return PublishConfig;
    }

    public static TomlTable? Read(Config config)
    {
        var configPath = config.GetProjectConfigPath();
        if (!File.Exists(configPath))
        {
            ThunderstoreCLI.Write.Warn(
                "Unable to find project configuration file",
                $"Looked from {Dim(configPath)}"
            );
            return null;
        }
        using var reader = new StreamReader(File.OpenRead(configPath));
        return TOML.Parse(reader);
    }

    public static void Write(Config config, string path)
    {
        var dependencies = config.PackageMeta.Dependencies ?? new Dictionary<string, string>();
        var copyPaths = config.BuildConfig.CopyPaths ?? new List<CopyPathMap>();
        var toml = new TomlTable
        {
            ["config"] =
            {
                ["schemaVersion"] = "0.0.1"
            },

            ["package"] = new TomlTable
            {
                ["namespace"] = config.PackageMeta.Namespace,
                ["name"] = config.PackageMeta.Name,
                ["versionNumber"] = config.PackageMeta.VersionNumber,
                ["description"] = config.PackageMeta.Description,
                ["websiteUrl"] = config.PackageMeta.WebsiteUrl,
                ["containsNsfwContent"] = config.PackageMeta.ContainsNsfwContent,
                ["dependencies"] = TomlUtils.DictToTomlTable(dependencies)
            },

            ["build"] = new TomlTable
            {
                ["icon"] = config.BuildConfig.IconPath,
                ["readme"] = config.BuildConfig.ReadmePath,
                ["outdir"] = config.BuildConfig.OutDir,
                ["copy"] = TomlUtils.BuildCopyPathTable(copyPaths)
            },

            ["publish"] = new TomlTable
            {
                ["repository"] = config.PublishConfig.Repository,
                ["communities"] = TomlUtils.FromArray(config.PublishConfig.Communities ?? new string[0]),
                ["categories"] = TomlUtils.FromArray(config.PublishConfig.Categories ?? new string[0])
            }
        };
        File.WriteAllText(path, TomlUtils.FormatToml(toml));
    }
}
