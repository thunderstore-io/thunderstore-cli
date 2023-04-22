use serde::{Serialize, Deserialize};

#[derive(Serialize, Deserialize, Debug)]
pub struct Package {
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

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageVersion {
    pub namespace: String,
    pub name: String,
    pub version_number: String,
    pub full_name: String,
    pub description: String,
    pub icon: String,
    pub dependencies: Vec<String>,
    pub download_url: String,
    pub downloads: u32,
    pub website_url: String,
    pub is_active: bool,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageListing {
    pub has_nsfw_content: bool,
    pub categories: Vec<String>,
    pub community: String,
    pub review_status: String,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageUpload {
    pub author_name: String,
    pub categories: Vec<String>,
    pub communities: Vec<String>,
    pub has_nsfw_content: bool,
    pub upload_uuid: String,
}
