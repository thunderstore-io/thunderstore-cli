use std::collections::HashMap;

use serde::{Deserialize, Serialize};

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
    schema_version: Version,
}

impl Default for ConfigData {
    fn default() -> Self {
        ConfigData {
            schema_version: "0.0.1".parse().unwrap(),
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
    pub dependencies: HashMap<String, Version>,
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
            dependencies: HashMap::from([(
                "AuthorName-PackageName".into(),
                "0.0.1".parse().unwrap(),
            )]),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct BuildData {
    icon: String,
    readme: String,
    outdir: String,
    copy: Vec<CopyPath>,
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
    source: String,
    target: String,
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
    repository: String,
    communities: Vec<String>,
    categories: Categories,
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
