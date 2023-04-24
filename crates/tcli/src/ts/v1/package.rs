use std::fmt::Display;
use crate::error::Error;
use crate::ts::v1::models::package::PackageListing;

use crate::ts::{CLIENT, CM, V1};

pub async fn get_all() -> Result<Vec<PackageListing>, Error> {
    let response = CLIENT.get(format!("{V1}/package/"))
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}

pub async fn get_community_all<T: Into<String> + Display>(community: T) -> Result<Vec<PackageListing>, Error> {
    let response = CLIENT.get(format!("{CM}/{community}/api/v1/package/"))
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}
