use std::fs;
use std::fs::File;
use std::io::Write;
use std::path::Path;

use crate::error::Error;
use crate::project::manifest::ProjectManifest;
use crate::ts::version::Version;

pub mod manifest;

pub fn create_new(
    config_path: impl AsRef<Path>,
    overwrite: bool,
    namespace: Option<String>,
    name: Option<String>,
    version: Option<Version>,
) -> Result<(), Error> {
    let config_path = config_path.as_ref();

    let project_dir = config_path.parent().unwrap_or("./".as_ref());
    let config_filename = config_path
        .file_name()
        .ok_or_else(|| Error::PathIsDirectory(config_path.into()))?;

    if project_dir.is_file() {
        return Err(Error::ProjectDirIsFile(project_dir.into()));
    }

    if !project_dir.is_dir() {
        fs::create_dir(project_dir).map_err(|e| Error::FileIoError(project_dir.into(), e))?;
    }

    let manifest_path = project_dir.join(config_filename);

    let manifest = {
        let mut manifest = ProjectManifest::default();
        if let Some(namespace) = namespace {
            manifest.package.namespace = namespace;
        }
        if let Some(name) = name {
            manifest.package.name = name;
        }
        if let Some(version) = version {
            manifest.package.version = version;
        }
        manifest
    };

    let mut options = File::options();
    options.write(true);
    if overwrite {
        options.create(true);
    } else {
        options.create_new(true);
    }

    let mut manifest_file = options
        .open(&manifest_path)
        .map_err(move |e| match e.kind() {
            std::io::ErrorKind::AlreadyExists => Error::ProjectAlreadyExists(manifest_path),
            _ => Error::FileIoError(manifest_path, e),
        })?;

    write!(
        manifest_file,
        "{}",
        toml::to_string_pretty(&manifest).unwrap()
    )
    .unwrap();

    let icon_path = project_dir.join("icon.png");
    File::create(&icon_path)
        .map_err(move |e| Error::FileIoError(icon_path, e))?
        .write_all(include_bytes!("../../resources/icon.png"))
        .unwrap();

    let readme_path = project_dir.join("README.md");
    write!(
        File::create(&readme_path).map_err(move |e| Error::FileIoError(readme_path, e))?,
        include_str!("../../resources/readme_template.md"),
        manifest.package.namespace,
        manifest.package.name,
        manifest.package.description
    )
    .unwrap();

    Ok(())
}
