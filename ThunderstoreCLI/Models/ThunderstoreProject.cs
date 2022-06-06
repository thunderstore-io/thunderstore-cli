using Tomlet.Attributes;

namespace ThunderstoreCLI.Models;

[TomlDoNotInlineObject]
public class ThunderstoreProject : BaseToml<ThunderstoreProject>
{
    [TomlDoNotInlineObject]
    public class ConfigData
    {
        [TomlProperty("schemaVersion")]
        public string SchemaVersion { get; set; } = "0.0.1";
    }
    [TomlProperty("config")]
    public ConfigData? Config { get; set; }

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
        public Dictionary<string, string> Dependencies { get; set; } = new()
        {
            { "AuthorName-PackageName", "0.0.1" }
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
        public CopyPath[] CopyPaths { get; set; } =
        {
            new()
        };
    }
    [TomlProperty("build")]
    public BuildData? Build { get; set; }

    [TomlDoNotInlineObject]
    public class PublishData
    {
        [TomlProperty("repository")]
        public string Repository { get; set; } = "https://thunderstore.io";
        [TomlProperty("communtities")]
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

        Config = new();
        Package = new();
        Build = new();
        Publish = new();
    }
}
