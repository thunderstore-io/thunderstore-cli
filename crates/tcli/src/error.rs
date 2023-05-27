use std::path::{Path, PathBuf};

#[allow(clippy::enum_variant_names)]
#[derive(Debug, thiserror::Error)]
#[repr(u32)]
pub enum Error {
    #[error("An API error occured.")]
    ApiError(#[from] reqwest::Error) = 1,

    #[error("The path at {0} is actually a file.")]
    ProjectDirIsFile(PathBuf),

    #[error("A project configuration already exists at {0}.")]
    ProjectAlreadyExists(PathBuf),

    #[error("A generic IO error occured: {0}")]
    GenericIoError(#[from] std::io::Error),

    #[error("A file IO error occured at path {0}: {1}")]
    FileIoError(PathBuf, std::io::Error),

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

    #[error("Project is missing required table '{0}'.")]
    MissingTable(&'static str),

    #[error("Missing repository url.")]
    MissingRepository,

    #[error("Missing auth token.")]
    MissingAuthToken,

    #[error("The package cache at {0} does not exist or is not writable.")]
    BadPackageCache(PathBuf),
}

pub trait IoResultToTcli<R> {
    fn map_fs_error(self, path: impl AsRef<Path>) -> Result<R, Error>;
}

impl<R> IoResultToTcli<R> for Result<R, std::io::Error> {
    fn map_fs_error(self, path: impl AsRef<Path>) -> Result<R, Error> {
        self.map_err(|e| Error::FileIoError(path.as_ref().into(), e))
    }
}

impl From<walkdir::Error> for Error {
    fn from(value: walkdir::Error) -> Self {
        Self::FileIoError(
            value.path().unwrap_or(Path::new("")).into(),
            value.into_io_error().unwrap(),
        )
    }
}
