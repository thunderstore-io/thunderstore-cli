use std::fs;
use std::path::PathBuf;
use tokio::process::Command;

use crate::package::install::manifest::InstallerManifest;
use crate::ui::reporter::{Progress, VoidProgress};
use crate::error::Error;

use self::runner::{InstallerArgs, InstallerResponse};

use super::Package;

pub mod runner;
pub mod manifest;
mod legacy_compat;

pub struct Installer {
    pub exec_path: PathBuf,
}

impl Installer {
    /// Loads the given package as an Installer and prepares it for execution.
    /// Note that cached installers can skip the prepare step.
    pub async fn load_and_prepare(package: &Package) -> Result<Installer, Error> {

        // Temp, we'll figure out a good solution from the progress reporter later.
        let test = VoidProgress {};
        let cache_dir = package.resolve(test.add_bar().as_ref()).await?;

        let manifest = {
            let path = cache_dir.join("installer.json");
            if !path.is_file() {
                Err(Error::MissingAuthToken)?
            } else {
                let contents = fs::read_to_string(path)?;
                serde_json::from_str::<InstallerManifest>(&contents)?
            }
        };

        let exec_path = cache_dir.join(manifest.executable);
        if !exec_path.is_file() {
            Err(Error::MissingAuthToken)?
        }

        Ok(Installer {
            exec_path
        })
    }

    pub async fn run(&self, args: &InstallerArgs) -> Result<InstallerResponse, Error> {
        let args_json = serde_json::to_string(args)?;
        let command = Command::new(&self.exec_path)
            .arg(&args_json)
            .spawn()
            .unwrap();

        todo!()
    }
}