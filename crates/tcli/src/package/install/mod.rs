use std::fs;
use std::io::{BufRead, BufReader};
use std::path::PathBuf;
use tokio::io::{AsyncRead, AsyncReadExt};
use tokio::process::Command;

use crate::package::install::manifest::InstallerManifest;
use crate::package::install::runner::request::RequestVariant;
use crate::ui::reporter::{Progress, VoidProgress};
use crate::error::Error;

use self::runner::INSTALLER_VERSION;
use self::runner::request::InstallerArgs;
use self::runner::response::{InstallResponse, ResponseVariant};

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

        let installer = Installer {
            exec_path
        };

        // Test that (a) the executable can be run and (b) it's on a valid inter-comm version.
        let version = match installer.run(&RequestVariant::Version).await? {
            ResponseVariant::Version { version } => version,
            _ => {
                panic!("The installer '{}' returned data not serializable into a Version response variant.", package.identifier)
            }
        };

        if version.major != INSTALLER_VERSION.major {
            Err(Error::PackageInstallerVersionMismatch {
                package_id: package.identifier.to_string(), 
                given_version: version, 
                our_version: INSTALLER_VERSION, 
            })?
        }

        Ok(installer)
    }

    pub async fn run(&self, arg: &RequestVariant) -> Result<ResponseVariant, Error> {
        let args_json = serde_json::to_string(arg)?;
        let mut child = Command::new(&self.exec_path)
            .arg(&args_json)
            .spawn()?;

        // Execute the installer, capturing and deserializing any output.
        // TODO: Safety check here to warn / stop an installer from blowing up the heap.
        let mut output_str = String::new();
        child.stdout.unwrap().read_to_string(&mut output_str).await?;

        todo!()
    }
}