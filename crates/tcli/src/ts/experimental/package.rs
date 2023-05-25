use crate::error::Error;
use crate::ts::experimental::models::package::{PackageListing, PackageVersion};
use crate::ts::version::Version;
use crate::ts::{CLIENT, EX};

pub async fn get_metadata(author: &str, name: &str) -> Result<PackageListing, Error> {
    Ok(CLIENT
        .get(format!("{EX}/package/{author}/{name}/"))
        .send()
        .await?
        .error_for_status()?
        .json()
        .await?)
}

pub async fn get_version_metadata(
    author: &str,
    name: &str,
    version: Version,
) -> Result<PackageVersion, Error> {
    Ok(CLIENT
        .get(format!("{EX}/package/{author}/{name}/{version}/"))
        .send()
        .await?
        .error_for_status()?
        .json()
        .await?)
}
