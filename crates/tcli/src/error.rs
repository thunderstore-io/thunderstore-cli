use std::path::PathBuf;

#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("An API error occured.")]
    ApiError(#[from] reqwest::Error),

    #[error("The path at {0} is actually a file.")]
    ProjectDirIsFile(PathBuf),

    #[error("A project configuration already exists at {0}.")]
    ProjectAlreadyExists(PathBuf),

    #[error("A generic IO error occured: {0}")]
    GenericIoError(#[from] std::io::Error),

    #[error("A file IO error occured at path {0}: {1}")]
    FileIoError(PathBuf, std::io::Error),

    #[error("Cannot remove manifest file at {0}.")]
    CannotRemoveManifest(PathBuf),

    #[error("The path {0} represents a directory.")]
    PathIsDirectory(PathBuf),

    #[error("Invalid version.")]
    InvalidVersion(#[from] crate::ts::version::VersionParseError),

    #[error("Failed to read project file. {0}")]
    FailedDeserializeProject(#[from] toml::de::Error),

    #[error("No project exists at the path {0}.")]
    NoProjectFile(PathBuf),

    #[error("Failed modifying zip file: {0}.")]
    ZipError(#[from] zip::result::ZipError),
}
