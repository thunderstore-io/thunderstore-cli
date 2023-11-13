use std::fs;
use std::path::PathBuf;

use once_cell::sync::Lazy;

use crate::error::IoResultToTcli;
use crate::ts::package_reference::PackageReference;
use crate::util::TempFile;
use crate::{Error, TCLI_HOME};

static CACHE_LOCATION: Lazy<PathBuf> = Lazy::new(|| TCLI_HOME.join("package_cache"));

pub async fn get_temp_zip_file(
    package: &PackageReference,
) -> Result<TempFile<tokio::fs::File>, Error> {
    fs::create_dir_all(CACHE_LOCATION.as_path()).map_fs_error(CACHE_LOCATION.as_path())?;
    let path = CACHE_LOCATION.join(format!("{package}.zip.tmp"));
    Ok(TempFile::open_async(path).await?)
}

pub fn get_cache_location(package: &PackageReference) -> PathBuf {
    CACHE_LOCATION.join(package.to_string())
}
