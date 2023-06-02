mod cache;
pub mod resolver;

use std::path::{Path, PathBuf};

use async_zip::tokio::read::seek::ZipFileReader;
use colored::Colorize;
use futures::prelude::*;
use tokio::fs::{self, File, OpenOptions};
use tokio::io::AsyncWriteExt;
use tokio_util::compat::TokioAsyncWriteCompatExt;

use crate::error::Error;
use crate::ts::experimental::package;
use crate::ts::package_manifest::PackageManifestV1;
use crate::ts::package_reference::PackageReference;
use crate::ts::CLIENT;
use crate::ui::reporter::{ProgressBarTrait};
use crate::TCLI_HOME;

#[derive(Debug)]
pub enum PackageSource {
    Remote(String),
    Local(PathBuf),
}

#[derive(Debug)]
pub struct Package {
    pub identifier: PackageReference,
    pub source: PackageSource,
    pub dependencies: Vec<PackageReference>,
}

impl Package {
    pub async fn resolve_new(ident: PackageReference) -> Result<Self, Error> {
        if let Ok(package_path) = cache::get_cached(&ident) {
            return Package::from_file(ident, &package_path).await;
        }

        Package::from_repo(ident).await
    }

    pub async fn from_file(ident: PackageReference, path: &Path) -> Result<Self, Error> {
        let mut package_file = File::open(path).await?;
        let mut zip = ZipFileReader::with_tokio(&mut package_file).await.unwrap();

        let manifest_entry = zip
            .file()
            .entries()
            .iter()
            .position(|x| x.entry().filename().as_str().unwrap() == "manifest.json")
            .expect("Package missing manifest.json, bailing.");

        let mut reader = zip.reader_with_entry(manifest_entry).await.unwrap();

        let mut manifest_str = String::new();
        reader.read_to_string(&mut manifest_str).await.unwrap();

        let manifest_str = manifest_str.trim_start_matches('\u{feff}');

        match serde_json::from_str::<PackageManifestV1>(manifest_str) {
            Ok(manifest) => Ok(Package {
                identifier: ident,
                source: PackageSource::Local(path.to_path_buf()),
                dependencies: manifest.dependencies,
            }),
            Err(_) => {
                println!(
                    "{} package \"{}\" has a malformed manifest, grabbing info from repo instead",
                    "[!]".bright_yellow(),
                    ident,
                );

                let mut package = Package::from_repo(ident).await?;
                package.source = PackageSource::Local(path.into());

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

    pub async fn add(&self, project: &Path, reporter: Box<dyn ProgressBarTrait>) -> Result<(), Error> {
        if matches!(self.source, PackageSource::Remote(_)) {
            self.download(reporter.as_ref()).await?;
        }

        let project_state = project.join("project_state");

        let package_path = cache::get_cached(&self.identifier)?;
        let install_dir = project_state.join(self.identifier.to_string());

        if install_dir.exists() {
            fs::remove_dir_all(&install_dir).await?;
        }

        let mut package_file = File::open(package_path).await?;
        let mut zip = ZipFileReader::with_tokio(&mut package_file).await.unwrap();

        let uncompressed_size: u64 = zip
            .file()
            .entries()
            .iter()
            .map(|x| x.entry().uncompressed_size())
            .sum();

        reporter.set_length(uncompressed_size as _);

        for index in 0..zip.file().entries().len() {
            let entry = zip.file().entries().get(index).unwrap().entry();

            if !entry.dir().unwrap() {
                let out_path = install_dir.join(entry.filename().as_str().unwrap());
                let out_parent = out_path.parent().unwrap();

                if !out_parent.exists() {
                    fs::create_dir_all(out_parent).await?;
                }

                let entry_len = entry.uncompressed_size();
                let mut entry_reader = zip.reader_without_entry(index).await.unwrap();

                let writer = OpenOptions::new()
                    .write(true)
                    .create_new(true)
                    .open(&out_path)
                    .await?;

                futures_util::io::copy(&mut entry_reader, &mut writer.compat_write())
                    .await
                    .unwrap();

                reporter.inc(entry_len as _);
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

    async fn download(&self, reporter: &dyn ProgressBarTrait) -> Result<(), Error> {
        let package_source = match &self.source {
            PackageSource::Remote(x) => x,
            _ => panic!("Invalid use, this is a local package."),
        };

        let download_result = CLIENT.get(package_source).send().await.unwrap();
        let download_size = download_result.content_length().unwrap();

        let progress_message = format!(
            "{}-{} ({})",
            self.identifier.namespace.bold(),
            self.identifier.name.bold(),
            self.identifier.version.to_string().truecolor(90, 90, 90)
        );

        reporter.set_length(download_size);
        reporter.set_message(progress_message);

        let mut download_stream = download_result.bytes_stream();

        let dest_path = TCLI_HOME
            .join("package_cache")
            .join(format!("{}.zip", self.identifier));
        fs::create_dir_all(dest_path.parent().unwrap()).await.unwrap();
        let mut outfile = File::create(dest_path).await.unwrap();

        while let Some(chunk) = download_stream.next().await {
            let chunk = chunk.unwrap();
            outfile.write_all(&chunk).await.unwrap();

            reporter.inc(chunk.len() as u64);
        }

        reporter.finish();

        Ok(())
    }
}
