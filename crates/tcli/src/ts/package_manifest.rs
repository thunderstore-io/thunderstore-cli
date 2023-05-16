use serde::{Deserialize, Serialize};

use crate::project::manifest::PackageData;
use crate::ts::package_reference::{self, PackageReference};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize, Debug)]
pub struct PackageManifestV1 {
    pub name: String,
    pub description: String,
    #[serde(rename = "version_number")]
    pub version: Version,
    #[serde(with = "package_reference::ser::string_array")]
    pub dependencies: Vec<PackageReference>,
    pub website_url: String,
}

impl PackageManifestV1 {
    pub fn from_manifest(value: PackageData, dependencies: Vec<PackageReference>) -> Self {
        Self {
            name: value.name,
            description: value.description,
            version: value.version,
            dependencies,
            website_url: value.website_url,
        }
    }
}
