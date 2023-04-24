use once_cell::sync::Lazy;
use reqwest::Client;
use reqwest::header::{ACCEPT, CONTENT_TYPE, HeaderMap, HeaderValue};

pub mod v1;
pub mod experimental;

pub(in crate::ts) const CM: &str = "https://thunderstore.io/c/";
pub(in crate::ts) const V1: &str = "https://thunderstore.io/api/v1";
pub(in crate::ts) const EX: &str = "https://thunderstore.io/api/experimental";

pub(in crate::ts) static CLIENT: Lazy<Client> = Lazy::new(|| {
    let mut header_map = HeaderMap::new();
    header_map.insert(CONTENT_TYPE, HeaderValue::from_static("application/json"));
    header_map.insert(ACCEPT, HeaderValue::from_static("application/json"));

    Client::builder()
        .default_headers(header_map)
        .build()
        .unwrap()
});
