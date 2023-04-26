use std::fmt::Display;

use crate::error::Error;
use crate::ts::experimental::models::package::{PackageListing, PackageVersion};
use crate::ts::{CLIENT, EX};

pub async fn get_metadata<T: Into<String> + Display>(
    author: T,
    name: T,
) -> Result<PackageListing, Error> {
    let response = CLIENT
        .get(format!("{EX}/package/{author}/{name}/"))
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}

pub async fn get_version_metadata<T: Into<String> + Display>(
    author: T,
    name: T,
    version: T,
) -> Result<PackageVersion, Error> {
    let response = CLIENT
        .get(format!("{EX}/package/{author}/{name}/{version}/"))
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}
