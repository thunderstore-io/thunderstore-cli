using System.Text.Json.Serialization;
using ThunderstoreCLI.Models;

namespace ThunderstoreCLI;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
public class PackageManifestV1 : BaseJson<PackageManifestV1, PackageManifestV1Context>
{
    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version_number")]
    public string? VersionNumber { get; set; }

    [JsonPropertyName("dependencies")]
    public string[]? Dependencies { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }
}

[JsonSerializable(typeof(PackageManifestV1))]
public partial class PackageManifestV1Context : JsonSerializerContext { }
