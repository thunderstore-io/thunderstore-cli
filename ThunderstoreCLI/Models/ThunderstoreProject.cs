using ThunderstoreCLI.Configuration;
using Tomlet;
using Tomlet.Attributes;
using Tomlet.Models;

namespace ThunderstoreCLI.Models;

[TomlDoNotInlineObject]
public class ThunderstoreProject : BaseToml<ThunderstoreProject>
{
    static ThunderstoreProject()
    {
        TomletMain.RegisterMapper<DictionaryWrapper>(
            instance =>
            {
                var table = new TomlTable
                {
                    ForceNoInline = true
                };
                foreach (var (key, val) in instance!.Wrapped)
                {
                    table.Entries.Add(key, new TomlString(val));
                }
                return table;
            },
            table =>
            {
                var dict = new Dictionary<string, string>();
                foreach (var (key, value) in ((TomlTable) table).Entries)
                {
                    dict[key] = value.StringValue;
                }
                return new DictionaryWrapper
                {
                    Wrapped = dict
                };
            }
        );
    }

    public class DictionaryWrapper
    {
        public required Dictionary<string, string> Wrapped { get; init; }
    }

    [TomlDoNotInlineObject]
    public class ConfigData
    {
        [TomlProperty("schemaVersion")]
        public string SchemaVersion { get; set; } = "0.0.1";
    }

    [TomlProperty("config")]
    public ConfigData? Config { get; set; } = new();

    [TomlDoNotInlineObject]
    public class PackageData
    {
        [TomlProperty("namespace")]
        public string Namespace { get; set; } = "AuthorName";
        [TomlProperty("name")]
        public string Name { get; set; } = "PackageName";
        [TomlProperty("versionNumber")]
        public string VersionNumber { get; set; } = "0.0.1";
        [TomlProperty("description")]
        public string Description { get; set; } = "Example mod description";
        [TomlProperty("websiteUrl")]
        public string WebsiteUrl { get; set; } = "https://thunderstore.io";
        [TomlProperty("containsNsfwContent")]
        public bool ContainsNsfwContent { get; set; } = false;

        [TomlProperty("dependencies")]
        public DictionaryWrapper Dependencies { get; set; } = new()
        {
            Wrapped = new Dictionary<string, string>() { { "AuthorName-PackageName", "0.0.1" } }
        };
    }
    [TomlProperty("package")]
    public PackageData? Package { get; set; }

    [TomlDoNotInlineObject]
    public class BuildData
    {
        [TomlProperty("icon")]
        public string Icon { get; set; } = "./icon.png";
        [TomlProperty("readme")]
        public string Readme { get; set; } = "./README.md";
        [TomlProperty("outdir")]
        public string OutDir { get; set; } = "./build";

        [TomlDoNotInlineObject]
        public class CopyPath
        {
            [TomlProperty("source")]
            public string Source { get; set; } = "./dist";
            [TomlProperty("target")]
            public string Target { get; set; } = "";
        }

        [TomlProperty("copy")]
        public CopyPath[] CopyPaths { get; set; } = new CopyPath[] { new CopyPath() };
    }
    [TomlProperty("build")]
    public BuildData? Build { get; set; }

    [TomlDoNotInlineObject]
    public class PublishData
    {
        [TomlProperty("repository")]
        public string Repository { get; set; } = "https://thunderstore.io";
        [TomlProperty("communities")]
        public string[] Communities { get; set; } =
        {
            "riskofrain2"
        };
        [TomlProperty("categories")]
        public string[] Categories { get; set; } =
        {
            "items", "skills"
        };
    }
    [TomlProperty("publish")]
    public PublishData? Publish { get; set; }

    public ThunderstoreProject() { }

    public ThunderstoreProject(bool initialize)
    {
        if (!initialize)
            return;

        Package = new PackageData();
        Build = new BuildData();
        Publish = new PublishData();
    }

    public ThunderstoreProject(Config config)
    {
        Package = new PackageData()
        {
            Namespace = config.PackageConfig.Namespace!,
            Name = config.PackageConfig.Name!
        };
        Build = new BuildData()
        {
            Icon = config.GetPackageIconPath(),
            OutDir = config.GetBuildOutputDir(),
            Readme = config.GetPackageReadmePath(),
            CopyPaths = config.BuildConfig.CopyPaths!.Select(x => new BuildData.CopyPath { Source = x.From, Target = x.To }).ToArray()!
        };
        Publish = new PublishData()
        {
            Categories = config.PublishConfig.Categories!,
            Communities = config.PublishConfig.Communities!,
            Repository = config.GeneralConfig.Repository
        };
    }
}
