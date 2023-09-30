use serde::{Serialize, Deserialize};

use crate::util::os::OS;

/// This manifest exists alongside the typical Thunderstore package manifest.
/// It contains additional information about the type of installer, the runner that will be used,
/// dependencies, etc.
#[derive(Serialize, Deserialize)]
pub struct InstallerManifest {
    pub executable: String,
    pub installer_version: u32,
    pub target_os: OS,
}