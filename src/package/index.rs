use std::borrow::Borrow;
use std::collections::HashMap;
use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::PathBuf;

use once_cell::sync::Lazy;

use crate::error::Error;
use crate::ts::experimental;
use crate::ts::experimental::index::PackageIndexEntry;
use crate::ts::package_reference::PackageReference;
use crate::TCLI_HOME;

static INDEX_PATH: Lazy<PathBuf> = Lazy::new(|| TCLI_HOME.join("package_index.json"));

/// An index which contains packages and optimized methods to query them.
pub struct PackageIndex {
    pub packages: Vec<PackageIndexEntry>,

    // We use indices here because self-referential structs are painful to implement in Rust.
    tight_index: HashMap<PackageReference, usize>,
    loose_index: HashMap<String, Vec<usize>>,
}

impl PackageIndex {
    /// Open and serialize the on-disk index, retrieving a fresh copy if it doesn't already exist.
    pub async fn open() -> Result<PackageIndex, Error> {
        if !INDEX_PATH.is_file() {
            sync_index().await?;
        }

        let mut index = PackageIndex {
            packages: {
                let str = fs::read_to_string(INDEX_PATH.as_path())?;
                serde_json::from_str(&str)?
            },
            tight_index: HashMap::new(),
            loose_index: HashMap::new(),
        };

        // Iterate through each package in the index, adding it to each map as necessary.
        for (i, package) in index.packages.iter().enumerate() {
            let package_ref = PackageReference::new(&package.namespace, &package.name, package.version).unwrap();
            let loose_ident = package_ref.to_loose_ident_string();

            index.tight_index.insert(package_ref, i);

            match index.loose_index.get_mut(&loose_ident) {
                Some(x) => x.push(i),
                None => {
                    index.loose_index.insert(loose_ident, vec![i]);
                }
            };
        }

        Ok(index)
    }

    /// Get a package which matches the given package reference.
    pub fn get_package(&self, reference: impl Borrow<PackageReference>) -> Option<&PackageIndexEntry> {
        self.packages
            .get(*self.tight_index.get(reference.borrow())?)
    }

    /// Get one or more packages that match the given loose package reference.
    pub fn get_packages(&self, loose_reference: String) -> Option<Vec<&PackageIndexEntry>> {
        Some(
            self.loose_index
                .get(&loose_reference)?
                .iter()
                .map(|x| &self.packages[*x])
                .collect::<Vec<_>>(),
        )
    }
}

/// Syncronizes the local TCLI cache with the remote repository.
pub async fn sync_index() -> Result<(), Error> {
    let entries = experimental::index::get_index().await?;
    let index_json = serde_json::to_string_pretty(&entries)?;

    let mut index_file = OpenOptions::new()
        .create(true)
        .write(true)
        .truncate(true)
        .open(INDEX_PATH.as_path())?;

    index_file.write_all(index_json.as_bytes())?;

    Ok(())
}
