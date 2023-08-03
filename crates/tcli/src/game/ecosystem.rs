use std::fs::File;
use std::io::{BufReader, BufWriter};

use crate::error::Error;
use crate::ts::v1::{ecosystem, models::ecosystem::EcosystemSchema};
use crate::TCLI_HOME;

pub async fn get_schema() -> Result<EcosystemSchema, Error> {
    let local_schema = TCLI_HOME.join("ecosystem_schema.json");

    match local_schema.is_file() {
        true => {
            let schema_file = File::open(&local_schema)?;
            let reader = BufReader::new(&schema_file);

            Ok(serde_json::from_reader(reader).unwrap())
        }

        false => {
            let schema_file = File::create(&local_schema)?;
            let schema = ecosystem::get_schema().await?;

            let schema_writer = BufWriter::new(&schema_file);
            serde_json::to_writer_pretty(schema_writer, &schema).unwrap();

            Ok(schema)
        }
    }
}
