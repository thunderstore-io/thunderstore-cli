use std::fmt::{Display, Formatter};

use once_cell::sync::{Lazy, OnceCell};
use reqwest::header::HeaderValue;
use reqwest::Client;

pub mod experimental;
pub mod package_manifest;
pub mod package_reference;
pub mod v1;
pub mod version;

pub struct RepositoryUrl(OnceCell<String>);

impl RepositoryUrl {
    const fn new() -> Self {
        Self(OnceCell::new())
    }
}

impl Display for RepositoryUrl {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "{}",
            self.0
                .get()
                .expect("Tried to get repository before initialization")
        )
    }
}

pub fn init_repository(repo: &str, auth_token: Option<&str>) {
    CM.0.set(format!("{repo}/c")).unwrap();
    V1.0.set(format!("{repo}/api/v1")).unwrap();
    EX.0.set(format!("{repo}/api/experimental")).unwrap();
    if let Some(auth) = auth_token {
        AUTH.set(
            HeaderValue::try_from(format!("Bearer {auth}")).expect("Invalid auth token format"),
        )
        .unwrap();
    }
}

pub(in crate::ts) static CM: RepositoryUrl = RepositoryUrl::new();
pub(in crate::ts) static V1: RepositoryUrl = RepositoryUrl::new();
pub(in crate::ts) static EX: RepositoryUrl = RepositoryUrl::new();
pub(in crate::ts) static AUTH: OnceCell<HeaderValue> = OnceCell::new();

pub(crate) static CLIENT: Lazy<Client> = Lazy::new(|| {
    Client::builder()
        .user_agent(concat!("thunderstore-cli/", env!("CARGO_PKG_VERSION")))
        .build()
        .unwrap()
});
