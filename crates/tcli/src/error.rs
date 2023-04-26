use std::path::PathBuf;

#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("An API error occured.")]
    ApiError(#[from] reqwest::Error),

    #[error("The path at {0} is actually a file.")]
    ProjectDirIsFile(PathBuf),

    #[error("A project configuration already exists at {0}.")]
    ProjectAlreadyExists(PathBuf),

    #[error("A file IO error occured.")]
    FileIoError(PathBuf, std::io::Error),

    #[error("Cannot remove manifest file at {0}.")]
    CannotRemoveManifest(PathBuf),

    #[error("The path {0} represents a directory.")]
    PathIsDirectory(PathBuf),

    #[error("Invalid version.")]
    InvalidVersion(#[from] crate::ts::version::VersionParseError),
}
