mod cache;
pub mod error;
pub mod index;
pub mod install;
pub mod resolver;

use std::io::{ErrorKind, Read, Seek};
use std::path::{Path, PathBuf};

use colored::Colorize;
use futures::prelude::*;
use serde::{Deserialize, Serialize};
use serde_with::{self, serde_as, DisplayFromStr};
use tokio::fs;
use tokio::io::{AsyncReadExt, AsyncWriteExt};

use crate::error::{Error, IoResultToTcli};
use crate::project::ProjectPath;
use crate::ts::experimental::package;
use crate::ts::package_manifest::PackageManifestV1;
use crate::ts::package_reference::PackageReference;
use crate::ts::CLIENT;
use crate::ui::reporter::{Progress, ProgressBarTrait};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub enum PackageSource {
    Remote(String),
    Local(PathBuf),
    Cache(PathBuf),
}

#[serde_as]
#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Package {
    pub source: PackageSource,
    #[serde_as(as = "DisplayFromStr")]
    pub identifier: PackageReference,
    #[serde(with = "crate::ts::package_reference::ser::string_array")]
    pub dependencies: Vec<PackageReference>,
}

impl Package {
    pub async fn resolve_new(ident: PackageReference) -> Result<Self, Error> {
        if cache::get_cache_location(&ident).exists() {
            return Package::from_cache(ident).await;
        }

        Package::from_repo(ident).await
    }

    pub async fn from_cache(ident: PackageReference) -> Result<Self, Error> {
        let path = cache::get_cache_location(&ident);
        let manifest_path = path.join("manifest.json");

        let mut manifest_str = String::new();
        fs::File::open(&manifest_path)
            .await
            .map_fs_error(&manifest_path)
            .unwrap()
            .read_to_string(&mut manifest_str)
            .await
            .unwrap();

        // Remove UTF-8 BOM: https://github.com/serde-rs/serde/issues/1753
        let manifest_str = manifest_str.trim_start_matches('\u{feff}');

        match serde_json::from_str::<PackageManifestV1>(manifest_str) {
            Ok(manifest) => Ok(Package {
                identifier: ident,
                source: PackageSource::Cache(path.to_path_buf()),
                dependencies: manifest.dependencies,
            }),
            Err(_) => {
                println!(
                    "{} package \"{}\" has a malformed manifest, grabbing info from repo instead",
                    "[!]".bright_yellow(),
                    ident,
                );

                let mut package = Package::from_repo(ident).await?;
                package.source = PackageSource::Cache(path.into());

                Ok(package)
            }
        }
    }

    pub async fn from_repo(ident: PackageReference) -> Result<Self, Error> {
        let package =
            package::get_version_metadata(&ident.namespace, &ident.name, ident.version).await?;

        Ok(Package {
            identifier: ident,
            source: PackageSource::Remote(package.download_url),
            dependencies: package.dependencies,
        })
    }

    /// Loads a package .zip from an arbitrary location. The package will be extracted into the cache,
    /// where it can then be worked with.
    pub async fn from_path(ident: PackageReference, path: &Path) -> Result<Self, Error> {
        let package =
            package::get_version_metadata(&ident.namespace, &ident.name, ident.version).await?;

        Ok(Package {
            identifier: ident,
            source: PackageSource::Local(path.to_path_buf()),
            dependencies: package.dependencies,
        })
    }

    pub async fn resolve(&self, reporter: &dyn ProgressBarTrait) -> Result<PathBuf, Error> {
        match &self.source {
            PackageSource::Local(path) => add_to_cache(
                &self.identifier,
                std::fs::File::open(path).map_fs_error(path)?,
            ),
            PackageSource::Remote(_) => self.download(reporter).await,
            PackageSource::Cache(path) => Ok(path.clone()),
        }
    }

    pub async fn add(
        &self,
        project: &ProjectPath,
        reporter: Box<dyn ProgressBarTrait>,
    ) -> Result<(), Error> {
        let cache_path = self.resolve(reporter.as_ref()).await?;
        let project_state = project.path().join("project_state");

        let install_dir = project_state.join(self.identifier.to_string());

        if install_dir.is_dir() {
            fs::remove_dir_all(&install_dir)
                .await
                .map_fs_error(&install_dir)?;
        }

        for item in walkdir::WalkDir::new(&cache_path).into_iter() {
            let item = item?;

            let dest_path = install_dir.join(item.path().strip_prefix(&cache_path).unwrap());

            if item.file_type().is_dir() {
                tokio::fs::create_dir_all(&dest_path)
                    .await
                    .map_fs_error(&dest_path)?;
            } else if item.file_type().is_file() {
                tokio::fs::copy(item.path(), &dest_path)
                    .await
                    .map_fs_error(&dest_path)?;
            }
        }

        let finished_msg = format!(
            "{} {}-{} ({})",
            "[✓]".green(),
            self.identifier.namespace.bold(),
            self.identifier.name.bold(),
            self.identifier.version.to_string().truecolor(90, 90, 90)
        );

        reporter.println(&finished_msg);
        reporter.finish_and_clear();

        Ok(())
    }

    async fn download(&self, reporter: &dyn ProgressBarTrait) -> Result<PathBuf, Error> {
        let PackageSource::Remote(package_source) = &self.source else {
            panic!("Invalid use, this is a local package.")
        };

        let output_path = cache::get_cache_location(&self.identifier);

        if output_path.is_dir() {
            reporter.finish();
            return Ok(output_path);
        }

        let download_result = CLIENT.get(package_source).send().await.unwrap();
        let download_size = download_result.content_length().unwrap();

        let progress_message = format!(
            "{}-{} ({})",
            self.identifier.namespace.bold(),
            self.identifier.name.bold(),
            self.identifier.version.to_string().truecolor(90, 90, 90)
        );

        reporter.set_length(download_size);
        reporter.set_message(format!("Downloading {progress_message}..."));

        let mut download_stream = download_result.bytes_stream();

        let mut temp_file = cache::get_temp_zip_file(&self.identifier).await?;
        let zip_file = temp_file.file_mut();

        while let Some(chunk) = download_stream.next().await {
            let chunk = chunk.unwrap();
            zip_file.write_all(&chunk).await.unwrap();

            reporter.inc(chunk.len() as u64);
        }

        reporter.set_message(format!("Unzipping {progress_message}..."));

        let cache_path = add_to_cache(&self.identifier, temp_file.into_std().await.file())?;

        reporter.finish();

        Ok(cache_path)
    }
}

fn add_to_cache(package: &PackageReference, zipfile: impl Read + Seek) -> Result<PathBuf, Error> {
    let output_path = cache::get_cache_location(package);

    match std::fs::remove_dir_all(&output_path) {
        Ok(_) => (),
        Err(e) if e.kind() == ErrorKind::NotFound => (),
        Err(e) => return Err(e).map_fs_error(&output_path),
    };

    std::fs::create_dir_all(&output_path).map_fs_error(&output_path)?;
    zip::read::ZipArchive::new(zipfile)?.extract(&output_path)?;

    Ok(output_path)
}
