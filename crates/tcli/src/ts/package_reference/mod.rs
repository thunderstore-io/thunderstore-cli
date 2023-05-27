use std::fmt::{Display, Formatter};
use std::str::FromStr;

use crate::ts::version::{Version, VersionParseError};

pub mod ser;

#[derive(Clone, Debug, PartialEq)]
pub struct PackageReference {
    pub namespace: String,
    pub name: String,
    pub version: Version,
}

impl PackageReference {
    pub fn new(
        namespace: impl AsRef<str>,
        name: impl AsRef<str>,
        version: Version,
    ) -> Result<Self, PackageReferenceParseError> {
        Ok(PackageReference {
            namespace: namespace.as_ref().to_string(),
            name: name.as_ref().to_string(),
            version,
        })
    }

    pub fn from_fullname_version(
        fullname: impl AsRef<str>,
        version: Version,
    ) -> Result<Self, PackageReferenceParseError> {
        let (name, namespace) =
            fullname
                .as_ref()
                .rsplit_once('-')
                .ok_or(PackageReferenceParseError::NumSections {
                    expected: 2,
                    got: 1,
                })?;
        Ok(PackageReference {
            namespace: namespace.to_string(),
            name: name.to_string(),
            version,
        })
    }

    pub fn to_loose_ident_string(&self) -> String {
        format!("{}-{}", self.namespace, self.name)
    }
}

#[derive(thiserror::Error, Debug)]
pub enum PackageReferenceParseError {
    #[error("Expected {expected} sections, got {got}.")]
    NumSections { expected: usize, got: usize },
    #[error("Failed to parse version: {0}.")]
    VersionParseFail(#[from] VersionParseError),
}

impl FromStr for PackageReference {
    type Err = PackageReferenceParseError;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let [version, name, namespace]: [&str; 3] = s
            .rsplitn(3, '-')
            .collect::<Vec<_>>()
            .try_into()
            .map_err(|v: Vec<&str>| PackageReferenceParseError::NumSections {
                expected: 3,
                got: v.len() - 1,
            })?;

        Ok(PackageReference {
            namespace: namespace.to_string(),
            name: name.to_string(),
            version: version.parse()?,
        })
    }
}

impl Display for PackageReference {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}-{}-{}", self.namespace, self.name, self.version)
    }
}
