use std::fs::{File, self};
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
    
    #[serde(skip_serializing_if = "Vec::is_empty")]
    pub publish: Vec<PublishData>,
    
    #[serde(flatten)]
    pub dependencies: DependencyData,
    
    #[serde(skip)]
    pub project_dir: Option<PathBuf>,
}

impl ProjectManifest {
    pub fn default_dev_project() -> Self {
        ProjectManifest {
            config: ConfigData {
                game: Some("risk-of-rain-2".to_string()),
                ..ConfigData::default()
            },
            package: Some(Default::default()),
            build: Some(Default::default()),
            publish: vec![Default::default()],
            dependencies: Default::default(),
            project_dir: None,
        }
    }

    pub fn default_profile_project() -> Self {
        ProjectManifest {
            config: Default::default(),
            package: None,
            build: None,
            publish: vec![],
            dependencies: Default::default(),
            project_dir: None,
        }
    }

    pub fn read_from_file(path: impl AsRef<Path>) -> Result<Self, Error> {
        let path = path.as_ref();
        let text = fs::read_to_string(path).map_err(|_| Error::NoProjectFile(path.into()))?;
      
        let mut manifest: ProjectManifest = toml::from_str(&text)?;
        
        manifest.project_dir = Some(
            path.parent()
                .map(|p| p.to_path_buf())
                .unwrap_or_else(|| PathBuf::from("./")),
        );
        Ok(manifest)
    }

    pub fn write_to_file(self, path: &Path) -> Result<(), Error> {
        let manifest_str = toml::to_string_pretty(&self)?;
        fs::write(path, manifest_str)?;

        Ok(())
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
        if let Some(repository) = overrides.repository {
            self.config.repository = Some(repository);
        }

        Ok(())
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct ConfigData {
    pub schema_version: Version,
    pub repository: Option<String>,
    pub game: Option<String>,
}

impl Default for ConfigData {
    fn default() -> Self {
        ConfigData {
            schema_version: Version::new(0, 0, 1),
            repository: Some("https://thunderstore.io".to_string()),
            game: None,
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
    #[serde(default)]
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
pub struct PublishData {
    pub community: String,
    pub categories: Vec<String>,
}

impl Default for PublishData {
    fn default() -> Self {
        PublishData {
            community: "riskofrain2".to_string(),
            categories: vec!["items".to_string(), "skills".to_string()],
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
    #[serde(skip_serializing_if = "Vec::is_empty")]
    pub dev_dependencies: Vec<PackageReference>,
}

impl Default for DependencyData {
    fn default() -> Self {
        DependencyData {
            dependencies: vec![],
            dev_dependencies: vec![],
        }
    }
}
