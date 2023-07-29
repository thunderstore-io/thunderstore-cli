use std::env::{self, VarError};
use std::fs::{self, File};
use std::io::Write;
use std::path::{Path, PathBuf};

use figment::Figment;
use figment::providers::{Format, Serialized, Toml, Env};

use serde::{Deserialize, Serialize};

use crate::error::Error;
use crate::TCLI_HOME;

pub enum Vars {
    HomeDir,
    PackageCacheDir,
}

impl Vars {
    pub fn into_var(self) -> Result<String, VarError> {
        env::var(self.as_str())
    }

    pub fn as_str(&self) -> &'static str {
        match self {
            Vars::HomeDir => "TCLI_HOME",
            Vars::PackageCacheDir => "TCLI_PACKAGE_CACHE",
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
pub struct Config {
    pub package_cache: PathBuf,
}

impl Default for Config {
    fn default() -> Self {
        Config {
            package_cache: TCLI_HOME.join("package_cache"),
        }
    }
}

impl Config {
    pub fn load(project_dir: &Path) -> Result<Self, figment::Error> {
        dbg!(project_dir.join("Config.toml"));
        
        Figment::new()
            .merge(Toml::file(TCLI_HOME.join("Config.toml")))
            .merge(Toml::file(project_dir.join("Config.toml")))
            .merge(Env::prefixed("TCLI_"))
            .join(Serialized::defaults(Config::default()))
            .extract()
    }
}
