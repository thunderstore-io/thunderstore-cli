use crate::error::Error;

use super::Runner;

pub struct PythonRunner;

impl Runner for PythonRunner {
    fn run(&self, args: Vec<String>) -> Result<(), Error> {
        todo!()
    }
}