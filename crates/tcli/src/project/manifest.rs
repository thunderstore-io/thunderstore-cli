use std::collections::HashMap;
use std::fs::File;
use std::io::Read;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

use crate::error::Error;
use crate::project::overrides::ProjectOverrides;
use crate::ts::package_reference::{self, PackageReference};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize, Debug)]
pub struct ProjectManifest {
    pub config: ConfigData,
    pub package: Option<PackageData>,
    pub build: Option<BuildData>,
    pub publish: Option<PublishData>,
    #[serde(flatten)]
    pub dependencies: DependencyData,
}

impl ProjectManifest {
    pub fn default_dev_project() -> Self {
        ProjectManifest {
            config: Default::default(),
            package: Some(Default::default()),
            build: Some(Default::default()),
            publish: Some(Default::default()),
            dependencies: Default::default(),
        }
    }

    pub fn read_from_file(path: impl AsRef<Path>) -> Result<Self, Error> {
        let mut text = String::new();
        File::open(&path)
            .map_err(|_| Error::NoProjectFile(path.as_ref().into()))?
            .read_to_string(&mut text)?;
        Ok(toml::from_str(&text)?)
    }

    pub fn apply_overrides(&mut self, overrides: ProjectOverrides) -> Result<(), Error> {
        if overrides.namespace.is_some() || overrides.name.is_some() || overrides.version.is_some()
        {
            let package = self
                .package
                .as_mut()
                .ok_or(Error::MissingTable("package"))?;
            if let Some(namespace) = overrides.namespace {
                package.namespace = namespace;
            }
            if let Some(name) = overrides.name {
                package.name = name;
            }
            if let Some(version) = overrides.version {
                package.version = version;
            }
        }
        if let Some(output_dir) = overrides.output_dir {
            self.build
                .as_mut()
                .ok_or(Error::MissingTable("build"))?
                .outdir = output_dir;
        }

        Ok(())
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct ConfigData {
    pub schema_version: Version,
    pub repository: String,
    pub game: String,
}

impl Default for ConfigData {
    fn default() -> Self {
        ConfigData {
            schema_version: Version::new(0, 0, 1),
            repository: "https://thunderstore.io".into(),
            game: "risk-of-rain2".into(),
        }
    }
}

#[derive(Serialize, Deserialize, Clone, Debug)]
#[serde(rename_all = "camelCase")]
pub struct PackageData {
    pub namespace: String,
    pub name: String,
    #[serde(rename = "versionNumber")]
    pub version: Version,
    pub description: String,
    pub website_url: String,
    pub contains_nsfw_content: bool,
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
    pub communities: Vec<String>,
    pub categories: Categories,
}

impl Default for PublishData {
    fn default() -> Self {
        PublishData {
            communities: vec!["riskofrain2".to_string()],
            categories: Categories::New(HashMap::from([(
                "riskofrain2".into(),
                vec!["items".into(), "skills".into()],
            )])),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct DependencyData {
    #[serde(default)]
    #[serde(with = "package_reference::ser::table")]
    pub dependencies: Vec<PackageReference>,
    #[serde(default)]
    #[serde(rename = "dev-dependencies")]
    #[serde(with = "package_reference::ser::table")]
    pub dev_dependencies: Vec<PackageReference>,
}

impl Default for DependencyData {
    fn default() -> Self {
        DependencyData {
            dependencies: vec![PackageReference::new(
                "AuthorName",
                "PackageName",
                Version::new(0, 0, 1),
            )
            .unwrap()],
            dev_dependencies: vec![],
        }
    }
}
