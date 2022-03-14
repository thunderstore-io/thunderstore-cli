using Newtonsoft.Json;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI;

public class PackageManifestV1 : BaseJson<PackageManifestV1>
{
    [JsonProperty("namespace")]
    public string? Namespace { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("version_number")]
    public string? VersionNumber { get; set; }

    [JsonProperty("dependencies")]
    public string[]? Dependencies { get; set; }

    [JsonProperty("website_url")]
    public string? WebsiteUrl { get; set; }
}
