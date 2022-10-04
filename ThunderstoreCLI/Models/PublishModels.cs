using Newtonsoft.Json;

namespace ThunderstoreCLI.Models;

public class PackageUploadMetadata : BaseJson<PackageUploadMetadata>
{
    [JsonProperty("author_name")] public string? AuthorName { get; set; }

    [JsonProperty("categories")] public string[]? Categories { get; set; }

    [JsonProperty("communities")] public string[]? Communities { get; set; }

    [JsonProperty("has_nsfw_content")] public bool HasNsfwContent { get; set; }

    [JsonProperty("upload_uuid")] public string? UploadUUID { get; set; }
}

public class UploadInitiateData : BaseJson<UploadInitiateData>
{
    public class UserMediaData
    {
        [JsonProperty("uuid")] public string? UUID { get; set; }

        [JsonProperty("filename")] public string? Filename { get; set; }

        [JsonProperty("size")] public long Size { get; set; }

        [JsonProperty("datetime_created")] public DateTime TimeCreated { get; set; }

        [JsonProperty("expiry")] public DateTime? ExpireTime { get; set; }

        [JsonProperty("status")] public string? Status { get; set; }
    }

    public class UploadPartData
    {
        [JsonProperty("part_number")] public int PartNumber { get; set; }

        [JsonProperty("url")] public string? Url { get; set; }

        [JsonProperty("offset")] public long Offset { get; set; }

        [JsonProperty("length")] public int Length { get; set; }
    }

    [JsonProperty("user_media")] public UserMediaData? Metadata { get; set; }

    [JsonProperty("upload_urls")] public UploadPartData[]? UploadUrls { get; set; }
}

public class FileData : BaseJson<FileData>
{
    [JsonProperty("filename")] public string? Filename { get; set; }

    [JsonProperty("file_size_bytes")] public long Filesize { get; set; }
}

public class CompletedUpload : BaseJson<CompletedUpload>
{
    public class CompletedPartData
    {
        [JsonProperty("ETag")] public string? ETag { get; set; }

        [JsonProperty("PartNumber")] public int PartNumber { get; set; }
    }

    [JsonProperty("parts")] public CompletedPartData[]? Parts { get; set; }
}

// JSON response structure for publish package request.
public class PublishData : BaseJson<PublishData>
{
    public class AvailableCommunityData
    {
        public class CommunityData
        {
            [JsonProperty("identifier")] public string? Identifier { get; set; }

            [JsonProperty("name")] public string? Name { get; set; }

            [JsonProperty("discord_url")] public string? DiscordUrl { get; set; }

            [JsonProperty("wiki_url")] public object? WikiUrl { get; set; }

            [JsonProperty("require_package_listing_approval")]
            public bool RequirePackageListingApproval { get; set; }
        }

        [JsonProperty("community")] public CommunityData? Community { get; set; }

        [JsonProperty("categories")] public List<string>? Categories { get; set; }

        [JsonProperty("url")] public string? Url { get; set; }
    }

    [JsonProperty("available_communities")]
    public List<AvailableCommunityData>? AvailableCommunities { get; set; }

    [JsonProperty("package_version")] public PackageVersionData? PackageVersion { get; set; }
}

public class PackageData : BaseJson<PackageData>
{
    [JsonProperty("namespace")] public string? Namespace { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("full_name")] public string? Fullname { get; set; }

    [JsonProperty("owner")] public string? Owner { get; set; }

    [JsonProperty("package_url")] public string? PackageUrl { get; set; }

    [JsonProperty("date_created")] public DateTime DateCreated { get; set; }

    [JsonProperty("date_updated")] public DateTime DateUpdated { get; set; }

    [JsonProperty("rating_score")] public string? RatingScore { get; set; }

    [JsonProperty("is_pinned")] public bool IsPinned { get; set; }

    [JsonProperty("is_deprecated")] public bool IsDeprecated { get; set; }

    [JsonProperty("total_downloads")] public string? TotalDownloads { get; set; }

    [JsonProperty("latest")] public PackageVersionData? LatestVersion { get; set; }

    [JsonProperty("community_listings")] public PackageListingData[]? CommunityListings { get; set; }
}

public class PackageVersionData : BaseJson<PackageVersionData>
{
    [JsonProperty("namespace")] public string? Namespace { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("version_number")] public string? VersionNumber { get; set; }

    [JsonProperty("full_name")] public string? FullName { get; set; }

    [JsonProperty("description")] public string? Description { get; set; }

    [JsonProperty("icon")] public string? Icon { get; set; }

    [JsonProperty("dependencies")] public List<string>? Dependencies { get; set; }

    [JsonProperty("download_url")] public string? DownloadUrl { get; set; }

    [JsonProperty("downloads")] public int Downloads { get; set; }

    [JsonProperty("date_created")] public DateTime DateCreated { get; set; }

    [JsonProperty("website_url")] public string? WebsiteUrl { get; set; }

    [JsonProperty("is_active")] public bool IsActive { get; set; }
}

public class PackageListingData : BaseJson<PackageListingData>
{
    [JsonProperty("has_nsfw_content")] public bool HasNsfwContent { get; set; }

    [JsonProperty("categories")] public string[]? Categories { get; set; }

    [JsonProperty("community")] public string? Community { get; set; }

    [JsonProperty("review_status")] public string? ReviewStatus { get; set; }
}
