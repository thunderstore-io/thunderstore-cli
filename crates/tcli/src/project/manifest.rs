use std::collections::HashMap;

use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Default)]
pub struct ProjectManifest {
    config: ConfigData,
    package: PackageData,
    build: BuildData,
    publish: PublishData,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct ConfigData {
    schema_version: String,
}

impl Default for ConfigData {
    fn default() -> Self {
        ConfigData {
            schema_version: "0.0.1".into(),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct PackageData {
    namespace: String,
    name: String,
    version_number: String,
    description: String,
    website_url: String,
    contains_nsfw_content: bool,
    dependencies: HashMap<String, String>,
}

impl Default for PackageData {
    fn default() -> Self {
        PackageData {
            namespace: "AuthorName".into(),
            name: "PackageName".into(),
            version_number: "0.0.1".into(),
            description: "Example mod description".into(),
            website_url: "https://thunderstore.io".into(),
            contains_nsfw_content: false,
            dependencies: HashMap::from([("AuthorName-PackageName".into(), "0.0.1".into())]),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct BuildData {
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
struct CopyPath {
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
enum Categories {
    Old(Vec<String>),
    New(HashMap<String, Vec<String>>),
}

#[derive(Serialize, Deserialize, Debug)]
struct PublishData {
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
