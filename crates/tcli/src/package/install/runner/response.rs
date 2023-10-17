use std::path::PathBuf;

use serde::{Deserialize, Serialize};

use crate::ts::version::Version;

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
