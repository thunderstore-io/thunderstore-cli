use serde::{Deserialize, Serialize};

use crate::ts::package_reference::{self, PackageReference};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageMetadata {
    pub namespace: String,
    pub name: String,
    pub full_name: String,
    pub owner: String,
    pub package_url: String,
    pub date_created: String,
    pub date_updated: String,
    pub rating_score: u32,
    pub is_pinned: bool,
    pub is_deprecated: bool,
    pub total_downloads: u32,
    pub latest: PackageVersion,
    pub community_listings: Vec<PackageListing>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct PackageVersion {
    pub namespace: String,
    pub name: String,
    #[serde(rename = "version_number")]
    pub version: Version,
    pub full_name: String,
    pub description: String,
    pub icon: String,
    #[serde(with = "package_reference::ser::string_array")]
    pub dependencies: Vec<PackageReference>,
    pub download_url: String,
    pub downloads: u32,
    pub website_url: String,
    pub is_active: bool,
}

impl From<crate::ts::v1::models::package::PackageVersion> for PackageVersion {
    fn from(value: crate::ts::v1::models::package::PackageVersion) -> Self {
        PackageVersion {
            namespace: value.full_name.rsplitn(3, '-').last().unwrap().to_string(),
            name: value.name,
            version: value.version,
            full_name: value.full_name,
            description: value.description,
            icon: value.icon,
            dependencies: value.dependencies,
            download_url: value.download_url,
            downloads: value.downloads,
            website_url: value.website_url,
            is_active: value.is_active,
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageListing {
    pub has_nsfw_content: bool,
    pub categories: Vec<String>,
    pub community: String,
    pub review_status: String,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageUploadMetadata {
    pub author_name: String,
    pub categories: Vec<String>,
    pub communities: Vec<String>,
    pub has_nsfw_content: bool,
    pub upload_uuid: String,
}
