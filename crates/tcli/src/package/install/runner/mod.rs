use crate::ts::version::Version;

pub mod request;
pub mod response;

pub static INSTALLER_VERSION: Version = Version {
    major: 1,
    minor: 0,
    patch: 0,
};
