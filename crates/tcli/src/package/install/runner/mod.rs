use std::path::PathBuf;
use serde::{Deserialize, Serialize};

use crate::{error::Error, package::Package};

use self::python::PythonRunner;

mod python;

/// Arguments are passed into the installer executable as a JSON string, not by argument
/// name-value pairs. This means that the installer's dev can rely on JSON deserialization
/// instead of a funky arg-parsing library.
#[derive(Serialize, Deserialize)]
pub struct InstallerArgs {
    arg_version: u32,
    package: Package,
    package_dir: PathBuf,
    game_dir: PathBuf,
}

pub struct InstallerResponse {
    thing: String
}

pub enum RunnerVariant {
    Python
}

impl RunnerVariant {
    pub fn into_runner(self) -> Box<dyn Runner> {
        Box::new(PythonRunner {})
    }
}

pub trait Runner {
    fn run(&self, args: Vec<String>) -> Result<(), Error>;
}