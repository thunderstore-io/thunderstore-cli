use std::path::PathBuf;

use crate::ts::package_reference::PackageReference;
use crate::{Error, TCLI_HOME};

/// Determine if the package exists within the local package cache.
pub fn package_exists(
    PackageReference {
        namespace,
        name,
        version,
    }: &PackageReference,
) -> Result<bool, Error> {
    let package_cache = TCLI_HOME.join("package_cache");
    let package_file = package_cache.join(format!("{namespace}-{name}-{version}.zip"));

    if !package_cache.exists() {
        return Err(Error::BadPackageCache(package_cache));
    }

    Ok(package_file.exists())
}

pub fn get_cached(package: &PackageReference) -> Result<PathBuf, Error> {
    let package_cache = TCLI_HOME.join("package_cache");
    let package_file = package_cache.join(format!("{}.zip", package));

    match package_file.exists() {
        true => Ok(package_file),
        false => Err(Error::BadPackageCache(package_file)),
    }
}
