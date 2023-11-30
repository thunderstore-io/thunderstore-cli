use serde::{Deserialize, Serialize};

use crate::ts::package_reference::{self, PackageReference};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageListing {
    pub name: String,
    pub full_name: String,
    pub owner: String,
    pub package_url: String,
    pub date_created: String,
    pub date_updated: String,
    pub uuid4: String,
    pub rating_score: u32,
    pub is_pinned: bool,
    pub is_deprecated: bool,
    pub has_nsfw_content: bool,
    pub categories: Vec<String>,
    pub versions: Vec<PackageVersion>,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageVersion {
    pub name: String,
    pub full_name: String,
    pub description: String,
    pub icon: String,
    #[serde(rename = "version_number")]
    pub version: Version,
    #[serde(with = "package_reference::ser::string_array")]
    pub dependencies: Vec<PackageReference>,
    pub download_url: String,
    pub downloads: u32,
    pub website_url: String,
    pub is_active: bool,
    pub date_created: String,
    pub uuid4: String,
    pub file_size: u64,
}
