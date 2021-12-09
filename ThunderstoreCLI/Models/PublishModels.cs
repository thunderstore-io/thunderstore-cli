using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ThunderstoreCLI.Models;

[ExcludeFromCodeCoverageAttribute]
public class PackageUploadMetadata : BaseJson<PackageUploadMetadata, PackageUploadMetadataContext>
{
    [JsonPropertyName("author_name")] public string? AuthorName { get; set; }

    [JsonPropertyName("categories")] public string[]? Categories { get; set; }

    [JsonPropertyName("communities")] public string[]? Communities { get; set; }

    [JsonPropertyName("has_nsfw_content")] public bool HasNsfwContent { get; set; }

    [JsonPropertyName("upload_uuid")] public string? UploadUUID { get; set; }
}

[JsonSerializable(typeof(PackageUploadMetadata))]
[ExcludeFromCodeCoverageAttribute]
public partial class PackageUploadMetadataContext : JsonSerializerContext { }

[ExcludeFromCodeCoverageAttribute]
public class UploadInitiateData : BaseJson<UploadInitiateData, UploadInitiateDataContext>
{
    public class UserMediaData
    {
        [JsonPropertyName("uuid")] public string? UUID { get; set; }

        [JsonPropertyName("filename")] public string? Filename { get; set; }

        [JsonPropertyName("size")] public long Size { get; set; }

        [JsonPropertyName("datetime_created")] public DateTime TimeCreated { get; set; }

        [JsonPropertyName("expiry")] public DateTime? ExpireTime { get; set; }

        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    public class UploadPartData
    {
        [JsonPropertyName("part_number")] public int PartNumber { get; set; }

        [JsonPropertyName("url")] public string? Url { get; set; }

        [JsonPropertyName("offset")] public long Offset { get; set; }

        [JsonPropertyName("length")] public int Length { get; set; }
    }

    [JsonPropertyName("user_media")] public UserMediaData? Metadata { get; set; }

    [JsonPropertyName("upload_urls")] public UploadPartData[]? UploadUrls { get; set; }
}

[JsonSerializable(typeof(UploadInitiateData))]
[ExcludeFromCodeCoverage]
public partial class UploadInitiateDataContext : JsonSerializerContext { }

[ExcludeFromCodeCoverageAttribute]
public class FileData : BaseJson<FileData, FileDataContext>
{
    [JsonPropertyName("filename")] public string? Filename { get; set; }

    [JsonPropertyName("file_size_bytes")] public long Filesize { get; set; }
}

[JsonSerializable(typeof(FileData))]
[ExcludeFromCodeCoverage]
public partial class FileDataContext : JsonSerializerContext { }

[ExcludeFromCodeCoverageAttribute]
public class CompletedUpload : BaseJson<CompletedUpload, CompletedUploadContext>
{
    public class CompletedPartData
    {
        [JsonPropertyName("ETag")] public string? ETag { get; set; }

        [JsonPropertyName("PartNumber")] public int PartNumber { get; set; }
    }

    [JsonPropertyName("parts")] public CompletedPartData[]? Parts { get; set; }
}

[JsonSerializable(typeof(CompletedUpload))]
public partial class CompletedUploadContext : JsonSerializerContext { }

// JSON response structure for publish package request.
[ExcludeFromCodeCoverageAttribute]
public class PublishData : BaseJson<PublishData, PublishDataContext>
{
    public class AvailableCommunityData
    {
        public class CommunityData
        {
            [JsonPropertyName("identifier")] public string? Identifier { get; set; }

            [JsonPropertyName("name")] public string? Name { get; set; }

            [JsonPropertyName("discord_url")] public string? DiscordUrl { get; set; }

            [JsonPropertyName("wiki_url")] public object? WikiUrl { get; set; }

            [JsonPropertyName("require_package_listing_approval")]
            public bool RequirePackageListingApproval { get; set; }
        }

        [JsonPropertyName("community")] public CommunityData? Community { get; set; }

        [JsonPropertyName("categories")] public List<string>? Categories { get; set; }

        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    public class PackageVersionData
    {
        [JsonPropertyName("namespace")] public string? Namespace { get; set; }

        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("version_number")] public string? VersionNumber { get; set; }

        [JsonPropertyName("full_name")] public string? FullName { get; set; }

        [JsonPropertyName("description")] public string? Description { get; set; }

        [JsonPropertyName("icon")] public string? Icon { get; set; }

        [JsonPropertyName("dependencies")] public List<string>? Dependencies { get; set; }

        [JsonPropertyName("download_url")] public string? DownloadUrl { get; set; }

        [JsonPropertyName("downloads")] public int Downloads { get; set; }

        [JsonPropertyName("date_created")] public DateTime DateCreated { get; set; }

        [JsonPropertyName("website_url")] public string? WebsiteUrl { get; set; }

        [JsonPropertyName("is_active")] public bool IsActive { get; set; }
    }

    [JsonPropertyName("available_communities")]
    public List<AvailableCommunityData>? AvailableCommunities { get; set; }

    [JsonPropertyName("package_version")] public PackageVersionData? PackageVersion { get; set; }
}

[JsonSerializable(typeof(PublishData))]
public partial class PublishDataContext : JsonSerializerContext { }
