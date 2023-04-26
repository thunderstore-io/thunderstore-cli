use std::fs;
use std::fs::{read, File};
use std::io::Write;
use std::path::Path;

use crate::error::Error;
use crate::project::manifest::ProjectManifest;

pub mod manifest;

pub async fn create_new(project_dir: impl AsRef<Path>, overwrite: bool) -> Result<(), Error> {
    let project_dir = project_dir.as_ref();

    if project_dir.is_file() {
        return Err(Error::ProjectDirIsFile(project_dir.into()));
    }

    if !project_dir.is_dir() {
        fs::create_dir(project_dir).unwrap();
    }

    let manifest = ProjectManifest::default();
    let manifest_path = project_dir.join("thunderstore.toml");

    match (manifest_path.is_file(), overwrite) {
        (true, true) => fs::remove_file(&manifest_path)
            .map_err(|_| Error::CannotRemoveManifest(manifest_path.clone().into())),
        (true, false) => return Err(Error::ProjectAlreadyExists(manifest_path.into())),
        (_, _) => Ok(()),
    }?;

    let manifest_toml =
        toml::to_string_pretty(&manifest).expect("Failed to serialize default thunderstore.toml");

    File::create(&manifest_path)
        .unwrap()
        .write_all(manifest_toml.as_bytes())
        .expect("Failed to write default project manifest file.");

    let icon_path = project_dir.join("icon.png");
    File::create(icon_path).expect("Failed to create default icon.png file.");

    let readme_path = project_dir.join("README.md");
    File::create(readme_path).expect("Failed to create default README.md file.");

    Ok(())
}
