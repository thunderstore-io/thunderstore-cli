use std::path::{Path, PathBuf};

#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("A generic API error occured.")]
    GenericApiError(reqwest::Error),

    #[error("Failed to serialize response as JSON.")]
    SerializationFailure(reqwest::Error),

    #[error("The path at {0} is actually a file.")]
    ProjectDirIsFile(PathBuf),

    #[error("A project configuration already exists at {0}.")]
    ProjectAlreadyExists(PathBuf),

    #[error("Cannot remove manifest file at {0}.")]
    CannotRemoveManifest(PathBuf)
}
