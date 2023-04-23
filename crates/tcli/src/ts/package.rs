use std::fmt::Display;
use reqwest::Client;
use reqwest::header::{ACCEPT, CONTENT_TYPE};
use crate::ts::error::Error;
use crate::ts::models::{ExPackageVersion, Package, V1PackageListing, V1PackageVersion};

const CM: &str = "https://thunderstore.io/c/";
const V1: &str = "https://thunderstore.io/api/v1";
const EX: &str = "https://thunderstore.io/api/experimental";

pub async fn get_metadata<T: Into<String> + Display>(author: T, name: T) -> Result<Package, Error> {
    let client = Client::new();
    let response = client.get(format!("{EX}/package/{author}/{name}/"))
        .header(CONTENT_TYPE, "application/json")
        .header(ACCEPT, "application/json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}

pub async fn get_version_metadata<T: Into<String> + Display>(author: T, name: T, version: T) -> Result<ExPackageVersion, Error> {
    let client = Client::new();
    let response = client.get(format!("{EX}/package/{author}/{name}/{version}/"))
        .header(CONTENT_TYPE, "application/json")
        .header(ACCEPT, "application/json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}

pub async fn get_all() -> Result<Vec<V1PackageListing>, Error> {
    let client = Client::new();
    let response = client.get(format!("{V1}/package/"))
        .header(CONTENT_TYPE, "application/json")
        .header(ACCEPT, "application/json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}

pub async fn get_community_all<T: Into<String> + Display>(community: T) -> Result<Vec<V1PackageListing>, Error> {
    let client = Client::new();
    let response = client.get(format!("{CM}/{community}/api/v1/package/"))
        .header(CONTENT_TYPE, "application/json")
        .header(ACCEPT, "application/json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}
