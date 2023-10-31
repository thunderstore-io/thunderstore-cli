use std::path::PathBuf;
use std::{env, fs};

use tokio::io::AsyncReadExt;
use tokio::process::Command;

use self::runner::response::Response;
use self::runner::INSTALLER_VERSION;
use super::error::Error;
use super::Package;
use crate::package::install::manifest::InstallerManifest;
use crate::package::install::runner::request::Request;
use crate::ui::reporter::{Progress, VoidProgress};

mod legacy_compat;
pub mod manifest;
pub mod runner;

pub struct Installer {
    pub exec_path: PathBuf,
}

impl Installer {
    /// Loads the given package as an Installer and prepares it for execution.
    /// Note that cached installers can skip the prepare step.
    pub async fn load_and_prepare(package: &Package) -> Result<Installer, crate::Error> {
        // Temp, we'll figure out a good solution from the progress reporter later.
        let test = VoidProgress {};
        let cache_dir = package.resolve(test.add_bar().as_ref()).await?;

        let manifest = {
            let path = cache_dir.join("installer.json");
            if !path.is_file() {
                Err(Error::InstallerNoManifest)?
            } else {
                let contents = fs::read_to_string(path)?;
                serde_json::from_str::<InstallerManifest>(&contents)?
            }
        };

        // Determine the absolute path of the installer's executable based on the current architecture.
        let current_arch = env::consts::ARCH;
        let current_os = env::consts::OS;

        let matrix = manifest
            .matrix
            .iter()
            .find(|x| {
                x.architecture.to_string() == current_arch && x.target_os.to_string() == current_os
            })
            .ok_or(Error::InstallerNotExecutable)?;

        let exec_path = {
            let abs = cache_dir.join(&matrix.executable);

            if abs.is_file() {
                Ok(abs)
            } else {
                Err(crate::Error::FileNotFound(abs))
            }
        }?;

        let installer = Installer { exec_path };

        // Validate that the installer is (a) executable and (b) is using a valid protocol version.
        let response = installer.run(&Request::Version).await?;
        let Response::Version { installer_version: _, protocol_version } = response else {
            Err(Error::InstallerBadResponse {
                package_id: package.identifier.to_string(),
                message: "The installer did not respond with a valid or otherwise serializable Version response variant.".to_string(),
            })?
        };

        if protocol_version.major != INSTALLER_VERSION.major {
            Err(Error::InstallerBadVersion {
                package_id: package.identifier.to_string(),
                given_version: protocol_version,
                our_version: INSTALLER_VERSION,
            })?
        }

        Ok(installer)
    }

    pub async fn run(&self, arg: &Request) -> Result<Response, crate::Error> {
        let args_json = serde_json::to_string(arg)?;
        let mut child = Command::new(&self.exec_path).arg(&args_json).spawn()?;

        // Execute the installer, capturing and deserializing any output.
        // TODO: Safety check here to warn / stop an installer from blowing up the heap.
        let mut output_str = String::new();
        child
            .stdout
            .unwrap()
            .read_to_string(&mut output_str)
            .await?;

        todo!()
    }
}
