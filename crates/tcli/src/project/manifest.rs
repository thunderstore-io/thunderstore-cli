use std::collections::HashMap;
use std::path::PathBuf;

use serde::{Deserialize, Serialize};

use crate::ts::package_reference::{self, PackageReference};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize, Default)]
pub struct ProjectManifest {
    pub config: ConfigData,
    pub package: PackageData,
    pub build: BuildData,
    pub publish: PublishData,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct ConfigData {
    pub schema_version: Version,
}

impl Default for ConfigData {
    fn default() -> Self {
        ConfigData {
            schema_version: Version::new(0, 0, 1),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct PackageData {
    pub namespace: String,
    pub name: String,
    #[serde(rename = "versionNumber")]
    pub version: Version,
    pub description: String,
    pub website_url: String,
    pub contains_nsfw_content: bool,
    #[serde(with = "package_reference::ser::table")]
    pub dependencies: Vec<PackageReference>,
}

impl Default for PackageData {
    fn default() -> Self {
        PackageData {
            namespace: "AuthorName".into(),
            name: "PackageName".into(),
            version: "0.0.1".parse().unwrap(),
            description: "Example mod description".into(),
            website_url: "https://thunderstore.io".into(),
            contains_nsfw_content: false,
            dependencies: vec![PackageReference::new(
                "AuthorName",
                "PackageName",
                Version::new(0, 0, 1),
            )
            .unwrap()],
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct BuildData {
    pub icon: PathBuf,
    pub readme: PathBuf,
    pub outdir: PathBuf,
    pub copy: Vec<CopyPath>,
}

impl Default for BuildData {
    fn default() -> Self {
        BuildData {
            icon: "./icon.png".into(),
            readme: "./README.md".into(),
            outdir: "./build".into(),
            copy: vec![CopyPath::default()],
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct CopyPath {
    pub source: PathBuf,
    pub target: PathBuf,
}

impl Default for CopyPath {
    fn default() -> Self {
        CopyPath {
            source: "./dist".into(),
            target: "".into(),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
// needs to be untagged to appear as transparent with no discrimator
#[serde(untagged)]
pub enum Categories {
    Old(Vec<String>),
    New(HashMap<String, Vec<String>>),
}

#[derive(Serialize, Deserialize, Debug)]
pub struct PublishData {
    pub repository: String,
    pub communities: Vec<String>,
    pub categories: Categories,
}

impl Default for PublishData {
    fn default() -> Self {
        PublishData {
            repository: "https://thunderstore.io".into(),
            communities: vec!["riskofrain2".to_string()],
            categories: Categories::New(HashMap::from([(
                "riskofrain2".into(),
                vec!["items".into(), "skills".into()],
            )])),
        }
    }
}
