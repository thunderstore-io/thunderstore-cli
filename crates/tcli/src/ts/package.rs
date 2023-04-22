use std::fmt::Display;
use reqwest::Client;
use reqwest::header::{ACCEPT, CONTENT_TYPE};
use crate::ts::error::Error;
use crate::ts::models::{Package, PackageVersion};

const V1: &str = "https://thunderstore.io/api/v1";
const XP: &str = "https://thunderstore.io/api/experimental";

pub async fn get_metadata<T: Into<String> + Display>(author: T, name: T) -> Result<Package, Error> {
    let client = Client::new();
    let response = client.get(format!("{XP}/package/{author}/{name}/"))
        .header(CONTENT_TYPE, "application/json")
        .header(ACCEPT, "application/json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}

pub async fn get_version_metadata<T: Into<String> + Display>(author: T, name: T, version: T) -> Result<PackageVersion, Error> {
    let client = Client::new();
    let response = client.get(format!("{XP}/package/{author}/{name}/{version}/"))
        .header(CONTENT_TYPE, "application/json")
        .header(ACCEPT, "application/json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}
