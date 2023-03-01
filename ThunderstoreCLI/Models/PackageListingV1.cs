using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ThunderstoreCLI.Commands;

namespace ThunderstoreCLI.Models;

public class PackageListingV1 : BaseJson<PackageListingV1>
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("full_name")]
    public string? Fullname { get; set; }

    [JsonProperty("owner")]
    public string? Owner { get; set; }

    [JsonProperty("package_url")]
    public string? PackageUrl { get; set; }

    [JsonProperty("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("date_updated")]
    public DateTime DateUpdated { get; set; }

    [JsonProperty("uuid4")]
    public string? Uuid4 { get; set; }

    [JsonProperty("rating_score")]
    public int RatingScore { get; set; }

    [JsonProperty("is_pinned")]
    public bool IsPinned { get; set; }

    [JsonProperty("is_deprecated")]
    public bool IsDeprecated { get; set; }

    [JsonProperty("has_nsfw_content")]
    public bool HasNsfwContent { get; set; }

    [JsonProperty("categories")]
    public string[]? Categories { get; set; }

    [JsonProperty("versions")]
    public PackageVersionV1[]? Versions { get; set; }

    public PackageListingV1() { }

    public PackageListingV1(PackageData package)
    {
        Name = package.Name;
        Fullname = package.Fullname;
        Owner = package.Namespace;
        PackageUrl = package.PackageUrl;
        DateCreated = package.DateCreated;
        DateUpdated = package.DateUpdated;
        Uuid4 = null;
        RatingScore = int.Parse(package.RatingScore!);
        IsPinned = package.IsPinned;
        IsDeprecated = package.IsDeprecated;
        HasNsfwContent = package.CommunityListings!.Any(l => l.HasNsfwContent);
        Categories = Array.Empty<string>();
        Versions = new[] { new PackageVersionV1(package.LatestVersion!) };
    }
}

public class PackageVersionV1
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("full_name")]
    public string? FullName { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("icon")]
    public string? Icon { get; set; }

    [JsonProperty("version_number")]
    public string? VersionNumber { get; set; }

    [JsonProperty("dependencies")]
    public string[]? Dependencies { get; set; }

    [JsonProperty("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonProperty("downloads")]
    public int Downloads { get; set; }

    [JsonProperty("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("uuid4")]
    public string? Uuid4 { get; set; }

    [JsonProperty("file_size")]
    public int FileSize { get; set; }

    [JsonIgnore]
    private GroupCollection? _fullNameParts;
    [JsonIgnore]
    public GroupCollection FullNameParts => _fullNameParts ??= InstallCommand.FullPackageNameRegex.Match(FullName!).Groups;

    public PackageVersionV1() { }

    public PackageVersionV1(PackageVersionData version)
    {
        Name = version.Name;
        FullName = version.FullName;
        Description = version.Description;
        Icon = version.Icon;
        VersionNumber = version.VersionNumber;
        Dependencies = version.Dependencies;
        DownloadUrl = version.DownloadUrl;
        Downloads = version.Downloads;
        DateCreated = version.DateCreated;
        WebsiteUrl = version.WebsiteUrl;
        IsActive = version.IsActive;
        Uuid4 = null;
        FileSize = 0;
    }
}
