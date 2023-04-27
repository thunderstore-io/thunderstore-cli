use std::collections::HashMap;

use serde::de::Error;
use serde::{Deserialize, Deserializer, Serialize, Serializer};

use crate::ts::package_reference::PackageReference;
use crate::ts::version::Version;

pub fn serialize<S: Serializer>(
    packages: impl AsRef<[PackageReference]>,
    ser: S,
) -> Result<S::Ok, S::Error> {
    packages
        .as_ref()
        .iter()
        .map(|p| (format!("{}-{}", p.namespace, p.name), p.version))
        .collect::<HashMap<_, _>>()
        .serialize(ser)
}

pub fn deserialize<'de, D: Deserializer<'de>, R: FromIterator<PackageReference>>(
    de: D,
) -> Result<R, D::Error> {
    HashMap::<String, Version>::deserialize(de)?
        .into_iter()
        .map(|(fullname, version)| PackageReference::from_fullname_version(fullname, version))
        .collect::<Result<_, _>>()
        .map_err(D::Error::custom)
}
