use std::path::PathBuf;

use serde::{Deserialize, Serialize};

use crate::{package::Package, ts::version::Version};

/// This is the minimum support 
pub static INSTALLER_VERSION: Version = Version {
    major: 1,
    minor: 0,
    patch: 0,
};

/// Arguments are passed into the installer executable as a JSON string, not by argument
/// name-value pairs. This means that the installer's dev can rely on JSON deserialization
/// instead of a funky arg-parsing library.
#[derive(Serialize, Deserialize)]
#[serde(tag = "type", content = "payload")]
pub enum Request {
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
    StartGame {
        mods_enabled: bool,
        project_state: PathBuf,
        game_dir: PathBuf,
        game_exe: PathBuf,
    },
}

#[derive(Serialize, Deserialize)]
#[serde(tag = "type", content = "payload")]
pub enum Response {
    Version {
        installer_version: Version,
        protocol_version: Version,
    },
    PackageInstall {
        tracked_files: Vec<PathBuf>,
    },
    PackageUninstall {
        tracked_files: Vec<PathBuf>,
    },
}
