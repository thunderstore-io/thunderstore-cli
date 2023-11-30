use std::env;
use std::fs::{self, File};
use std::io::Write;
use std::path::{PathBuf, Path};
use std::process::Stdio;

use colored::Colorize;
use tokio::io::AsyncReadExt;
use tokio::process::Command;

use self::api::Request;
use self::api::Response;
use self::api::PROTOCOL_VERSION;
use self::manifest::InstallerManifest;
use super::{Package, PackageSource};
use crate::ui::reporter::{Progress, VoidProgress, ProgressBarTrait};
use crate::Error;

pub mod api;
mod legacy_compat;
pub mod manifest;

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
        let Response::Version { author, identifier, protocol } = response else {
            Err(Error::InstallerBadResponse {
                package_id: package.identifier.to_string(),
                message: "The installer did not respond with a valid or otherwise serializable Version response variant.".to_string(),
            })?
        };

        if protocol.major != PROTOCOL_VERSION.major {
            Err(Error::InstallerBadVersion {
                package_id: package.identifier.to_string(),
                given_version: protocol,
                our_version: PROTOCOL_VERSION,
            })?
        }

        Ok(installer)
    }

    pub fn dummy_new() -> Self {
        let dummy_installer = PathBuf::from(std::env::var("TCLI_DUMMY_INSTALLER").unwrap());

        if !dummy_installer.is_file() {
            panic!(
                "TCLI_DUMMY_INSTALLER is set to {}, which does not point to a file that actually exists.", dummy_installer.to_str().unwrap()
            )
        }
        
        Installer {
            exec_path: dummy_installer
        }
    }

    pub async fn install_package(
        &self, 
        package: &Package, 
        package_dir: &Path, 
        state_dir: &Path, 
        game_dir: &Path, 
        reporter: &dyn ProgressBarTrait
    ) -> Result<Vec<PathBuf>, Error> {  
        // Determine if the package is a modloader or not.
        let is_modloader = package.identifier.name.to_lowercase().contains("bepinex");
                
        let request = Request::PackageInstall {
            is_modloader,
            package: package.identifier.clone(),
            package_deps: package.dependencies.clone(),
            package_dir: package_dir.to_path_buf(),
            state_dir: state_dir.to_path_buf(),
            game_dir: game_dir.to_path_buf(),
        };

        let progress_message = format!(
            "{}-{} {}",
            package.identifier.namespace.bold(),
            package.identifier.name.bold(),
            package.identifier.version.to_string().truecolor(90, 90, 90)
        );

        reporter.set_message(format!("Installing {progress_message}"));
        
        let response = self.run(&request).await?;

        match response {
            Response::PackageInstall { tracked_files } => {
                Ok(tracked_files)
            }

            Response::Error { message } => {
                Err(Error::InstallerError { message  })
            }

            x => {
                let message = 
                    format!("Didn't recieve one of the expected variants: Response::PackageInstall or Response::Error. Got: {x:#?}");
                
                Err(Error::InstallerBadResponse { package_id: format!("yes"), message })
            }
        }
    }

    /// Start the game and drop a PID file in the state directory of the current project.
    pub async fn start_game(&self, mods_enabled: bool, state_dir: &Path, game_dir: &Path, game_exe: &Path, args: Vec<String>) -> Result<u32, Error> {
        let request = Request::StartGame {
            mods_enabled,
            project_state: state_dir.to_path_buf(),
            game_dir: game_dir.to_path_buf(),
            game_exe: game_exe.to_path_buf(),
            args,
        };

        let response = self.run(&request).await?;

        let Response::StartGame { pid } = response else {
            panic!("Invalid response.");
        };

        Ok(pid)
    }

    pub async fn run(&self, arg: &Request) -> Result<Response, Error> {
        let args_json = serde_json::to_string(arg)?;
        
        let child = Command::new(&self.exec_path)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .arg(&args_json)
            .spawn()?;
        
        // Execute the installer, capturing and deserializing any output.
        // TODO: Safety check here to warn / stop an installer from blowing up the heap.
        let mut output_str = String::new();
        child
            .stdout
            .unwrap()
            .read_to_string(&mut output_str)
            .await?;

        let mut err_str = String::new();
        child
            .stderr
            .unwrap()
            .read_to_string(&mut err_str)
            .await?;

        // println!("installer stdout:");
        // println!("{output_str}");

        let response = serde_json::from_str(&output_str)?;
        Ok(response)
    }
}
