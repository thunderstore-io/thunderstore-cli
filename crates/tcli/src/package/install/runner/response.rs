use std::path::PathBuf;
use serde::{Serialize, Deserialize};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize)]
#[serde(tag = "type", content = "payload")]
pub enum ResponseVariant {
    Version {
        version: Version,
    },
    InstallResponse {
        tracked_files: Vec<PathBuf>,
    },
    UninstallResponse {
        tracked_files: Vec<PathBuf>,
    },
}

#[derive(Serialize, Deserialize)]
pub struct VersionResponse {
    version: Version,
}

#[derive(Serialize, Deserialize)]
pub struct InstallResponse {
    tracked_files: Vec<PathBuf>,
}

#[derive(Serialize, Deserialize)]
pub struct UninstallResponse {
    tracked_files: Vec<PathBuf>,
}
