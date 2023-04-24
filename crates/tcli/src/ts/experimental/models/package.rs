use serde::{Serialize, Deserialize};

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

impl Into<PackageVersion> for crate::ts::v1::models::package::PackageVersion {
    fn into(self) -> PackageVersion {
        PackageVersion {
            namespace: self.full_name.rsplitn(3, '-').last().unwrap().to_string(),
            name: self.name,
            version_number: self.version_number,
            full_name: self.full_name,
            description: self.description,
            icon: self.icon,
            dependencies: self.dependencies,
            download_url: self.download_url,
            downloads: self.downloads,
            website_url: self.website_url,
            is_active: self.is_active,
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
