using Tommy;
using static Crayon.Output;

namespace ThunderstoreCLI.Config;

class ProjectFileConfig : EmptyConfig
{
    private GeneralConfig? GeneralConfig { get; set; }

    private PackageConfig? PackageMeta { get; set; }

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

        GeneralConfig = ParseGeneralConfig(tomlData);
        PackageMeta = ParsePackageMeta(tomlData);
        BuildConfig = ParseBuildConfig(tomlData);
        PublishConfig = ParsePublishConfig(tomlData);
    }

    protected static GeneralConfig? ParseGeneralConfig(TomlTable tomlData)
    {
        if (!tomlData.HasKey("publish"))
            return null;

        return new GeneralConfig()
        {
            Repository = TomlUtils.SafegetString(tomlData["publish"], "repository")!
        };
    }

    protected static PackageConfig? ParsePackageMeta(TomlTable tomlData)
    {
        if (!tomlData.HasKey("package"))
            return null;

        var packageMeta = tomlData["package"];

        // TODO: Add warnings on missing values
        var result = new PackageConfig()
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
            Communities = TomlUtils.SafegetStringArray(publishConfig, "communities", Array.Empty<string>()),
            Categories = TomlUtils.SafegetStringArray(publishConfig, "categories", Array.Empty<string>())
        };
    }

    public override GeneralConfig? GetGeneralConfig()
    {
        return GeneralConfig;
    }

    public override PackageConfig? GetPackageMeta()
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
        var dependencies = config.PackageConfig.Dependencies ?? new Dictionary<string, string>();
        var copyPaths = config.BuildConfig.CopyPaths ?? new List<CopyPathMap>();
        var toml = new TomlTable
        {
            ["config"] =
            {
                ["schemaVersion"] = "0.0.1"
            },

            ["package"] = new TomlTable
            {
                ["namespace"] = config.PackageConfig.Namespace,
                ["name"] = config.PackageConfig.Name,
                ["versionNumber"] = config.PackageConfig.VersionNumber,
                ["description"] = config.PackageConfig.Description,
                ["websiteUrl"] = config.PackageConfig.WebsiteUrl,
                ["containsNsfwContent"] = config.PackageConfig.ContainsNsfwContent,
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
                ["repository"] = config.GeneralConfig.Repository,
                ["communities"] = TomlUtils.FromArray(config.PublishConfig.Communities ?? new string[0]),
                ["categories"] = TomlUtils.FromArray(config.PublishConfig.Categories ?? new string[0])
            }
        };
        File.WriteAllText(path, TomlUtils.FormatToml(toml));
    }
}
