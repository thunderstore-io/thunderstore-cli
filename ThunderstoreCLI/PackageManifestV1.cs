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

    [JsonProperty("installers")]
    public InstallerDeclaration[]? Installers { get; set; }

    private string? fullName;
    public string FullName => fullName ??= $"{Namespace}-{Name}";

    public class InstallerDeclaration
    {
        [JsonProperty("identifier")]
        public string? Identifier { get; set; }
    }

    public PackageManifestV1() { }

    public PackageManifestV1(PackageVersionData version)
    {
        Namespace = version.Namespace;
        Name = version.Name;
        Description = version.Description;
        VersionNumber = version.VersionNumber;
        Dependencies = version.Dependencies?.ToArray() ?? Array.Empty<string>();
        WebsiteUrl = version.WebsiteUrl;
    }

    public PackageManifestV1(PackageListingV1 listing, PackageVersionV1 version)
    {
        Namespace = listing.Owner;
        Name = listing.Name;
        Description = version.Description;
        VersionNumber = version.VersionNumber;
        Dependencies = version.Dependencies?.ToArray() ?? Array.Empty<string>();
        WebsiteUrl = version.WebsiteUrl;
    }
}
