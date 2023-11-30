use std::fmt::{Display, Formatter};
use std::str::FromStr;

use serde_with::{DeserializeFromStr, SerializeDisplay};

#[derive(
    SerializeDisplay, DeserializeFromStr, Debug, Copy, Clone, PartialEq, Eq, PartialOrd, Ord, Hash,
)]
pub struct Version {
    pub major: u32,
    pub minor: u32,
    pub patch: u32,
}

impl Version {
    pub const fn new(major: u32, minor: u32, patch: u32) -> Version {
        Version {
            major,
            minor,
            patch,
        }
    }
}

#[derive(Debug, thiserror::Error)]
pub enum VersionParseError {
    #[error("Failed to parse an integer because: {0}.")]
    IntParse(#[from] std::num::ParseIntError),
    #[error("Expected 2 dots in version string, got {0}.")]
    DotCount(usize),
}

impl FromStr for Version {
    type Err = VersionParseError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let [major, minor, patch]: [u32; 3] = s
            .splitn(3, '.')
            .map(|n| n.parse())
            .collect::<Result<Vec<_>, _>>()?
            .try_into()
            .map_err(|v: Vec<u32>| VersionParseError::DotCount(v.len() - 1))?;

        Ok(Version {
            major,
            minor,
            patch,
        })
    }
}

impl Display for Version {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}.{}.{}", self.major, self.minor, self.patch)
    }
}
