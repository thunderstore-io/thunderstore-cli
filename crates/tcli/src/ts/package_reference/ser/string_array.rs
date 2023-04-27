use serde::de::Error;
use serde::{Deserialize, Deserializer, Serialize, Serializer};

use crate::ts::package_reference::PackageReference;

pub fn serialize<S: Serializer>(
    packages: impl AsRef<[PackageReference]>,
    ser: S,
) -> Result<S::Ok, S::Error> {
    packages
        .as_ref()
        .iter()
        .map(|p| p.to_string())
        .collect::<Vec<_>>()
        .serialize(ser)
}

pub fn deserialize<'de, D: Deserializer<'de>, R: FromIterator<PackageReference>>(
    de: D,
) -> Result<R, D::Error> {
    Vec::<String>::deserialize(de)?
        .into_iter()
        .map(|package_ref| package_ref.parse())
        .collect::<Result<_, _>>()
        .map_err(D::Error::custom)
}
