use std::path::PathBuf;
use serde::{Serialize, Deserialize};

use crate::package::Package;

/// Arguments are passed into the installer executable as a JSON string, not by argument
/// name-value pairs. This means that the installer's dev can rely on JSON deserialization
/// instead of a funky arg-parsing library.
#[derive(Serialize, Deserialize)]
#[serde(tag = "type", content = "payload")]
pub enum RequestVariant {
    Version,
    PackageInstall {
        package: Package,
        package_dir: PathBuf,
        game_dir: PathBuf,
    },
    PackageUninstall {
        package: Package,
        game_dir: PathBuf,
        tracked_files: Vec<PathBuf>,
    },
}

#[derive(Serialize, Deserialize)]
pub struct ArgHeader {
    version: u32,
    payload: ArgVariants,
}

#[derive(Serialize, Deserialize)]
pub enum ArgVariants {
    InstallerVersion,
    InstallArgs(InstallerArgs),
}

#[derive(Serialize, Deserialize)]
pub struct InstallerArgs {
    arg_version: u32,
    package: Package,
    package_dir: PathBuf,
    game_dir: PathBuf,
}
