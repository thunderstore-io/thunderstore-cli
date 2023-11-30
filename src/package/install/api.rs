use std::path::PathBuf;

use serde::{Deserialize, Serialize};

use crate::ts::version::Version;
use crate::ts::package_reference::PackageReference;

/// This is the minimum support 
pub static PROTOCOL_VERSION: Version = Version {
    major: 1,
    minor: 0,
    patch: 0,
};

/// Arguments are passed into the installer executable as a JSON string, not by argument
/// name-value pairs. This means that the installer's dev can rely on JSON deserialization
/// instead of a funky arg-parsing library.
#[derive(Serialize, Deserialize, Debug)]
#[serde(tag = "type", content = "payload")]
pub enum Request {
    Version,
    PackageInstall {
        is_modloader: bool,
        package: PackageReference,
        package_deps: Vec<PackageReference>,
        package_dir: PathBuf,
        state_dir: PathBuf,
        game_dir: PathBuf,
    },
    PackageUninstall {
        is_modloader: bool,
        package: PackageReference,
        package_deps: Vec<PackageReference>,
        package_dir: PathBuf,
        state_dir: PathBuf,
        game_dir: PathBuf,
        tracked_files: Vec<PathBuf>,
    },
    StartGame {
        mods_enabled: bool,
        project_state: PathBuf,
        game_dir: PathBuf,
        game_exe: PathBuf,
        args: Vec<String>,
    },
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(tag = "type", content = "payload")]
pub enum Response {
    Version {
        author: String,
        identifier: PackageReference,
        protocol: Version,
    },
    PackageInstall {
        tracked_files: Vec<PathBuf>,
    },
    PackageUninstall {
        tracked_files: Vec<PathBuf>,
    },
    StartGame {
        pid: u32,  
    },
    Error {
        message: String,
    }
}
