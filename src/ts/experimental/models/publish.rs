use std::collections::HashMap;

use serde::{Deserialize, Serialize};

use crate::ts::experimental::models::package::PackageVersion;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserMediaInitiateUploadParams {
    pub filename: String,
    pub file_size_bytes: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserMediaInitiateUploadResponse {
    pub user_media: UserMedia,
    pub upload_urls: Vec<UploadPartUrl>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserMedia {
    pub uuid: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UploadPartUrl {
    pub part_number: u32,
    pub url: String,
    pub offset: u64,
    pub length: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserMediaFinishUploadParams {
    pub parts: Vec<CompletedPart>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompletedPart {
    #[serde(rename = "ETag")]
    pub etag: String,
    #[serde(rename = "PartNumber")]
    pub part_number: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PackageSubmissionMetadata {
    pub author_name: String,
    pub communities: Vec<String>,
    pub has_nsfw_content: bool,
    pub upload_uuid: String,
    pub community_categories: HashMap<String, Vec<String>>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct PackageSubmissionResult {
    pub package_version: PackageVersion,
    pub available_communities: Vec<AvailableCommunity>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct AvailableCommunity {
    pub community: Community,
    pub url: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct Community {
    pub identifier: String,
    pub name: String,
    pub discord_url: Option<String>,
    pub wiki_url: Option<String>,
    pub require_package_listing_approval: bool,
}
