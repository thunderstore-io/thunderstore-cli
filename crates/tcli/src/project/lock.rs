use std::collections::HashMap;
use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Deserializer, Serialize, Serializer};

use crate::package::Package;
use crate::Error;

#[derive(Serialize, Deserialize, Debug)]
pub struct LockFile {
    #[serde(skip)]
    path: PathBuf,

    version: u32,

    #[serde(with = "crate::project::lock")]
    pub packages: HashMap<String, Package>,
}

impl LockFile {
    /// Opens and reads or creates a new lockfile instance.
    pub fn open_or_new(path: impl AsRef<Path>) -> Result<Self, Error> {
        let path = path.as_ref();

        if path.exists() {
            let contents = fs::read_to_string(path)?;
            let lockfile = serde_json::from_str(&contents).unwrap();

            Ok(LockFile {
                path: path.to_path_buf(),
                ..lockfile
            })
        } else {
            Ok(LockFile {
                version: 1,
                path: path.to_path_buf(),
                packages: HashMap::new(),
            })
        }
    }

    /// Merges one or more packages into the lockfile, overwriting as needed.
    pub fn merge(&mut self, packages: &[Package]) {
        let new_packages = packages
            .iter()
            .map(|package| (package.identifier.to_loose_ident_string(), package.clone()))
            .collect::<HashMap<_, _>>();

        self.packages.extend(new_packages);
    }

    /// Writes the lockfile to disk.
    pub fn commit(self) -> Result<(), Error> {
        let mut lockfile = OpenOptions::new()
            .create(true)
            .write(true)
            .open(&self.path)?;

        let new_contents = serde_json::to_string_pretty(&self).unwrap();
        lockfile.write_all(new_contents.as_bytes())?;

        Ok(())
    }
}

pub fn serialize<S: Serializer>(
    packages: &HashMap<String, Package>,
    ser: S,
) -> Result<S::Ok, S::Error> {
    packages.values().collect::<Vec<_>>().serialize(ser)
}

pub fn deserialize<'de, D: Deserializer<'de>>(de: D) -> Result<HashMap<String, Package>, D::Error> {
    Ok(Vec::<Package>::deserialize(de)?
        .into_iter()
        .map(|package| (package.identifier.to_loose_ident_string(), package))
        .collect::<HashMap<_, _>>())
}
