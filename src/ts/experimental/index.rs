use async_compression::futures::bufread::GzipDecoder;
use futures::{TryStreamExt, AsyncBufReadExt, StreamExt};
use futures::io::{self, BufReader, ErrorKind};
use serde::{Serialize, Deserialize};

use crate::Error;
use crate::ts::package_reference::PackageReference;
use crate::ts::{CLIENT, EX};
use crate::ts::version::Version;

#[derive(Serialize, Deserialize)]
pub struct PackageIndexEntry {
	pub namespace: String,
	pub name: String,
	#[serde(rename = "version_number")]
	pub version: Version,
	pub file_format: Option<String>,
	pub file_size: usize,
	pub dependencies: Vec<PackageReference>,
}

pub async fn get_index() -> Result<Vec<PackageIndexEntry>, Error> {
	let response = CLIENT
		.get(format!("{EX}/package-index"))
		.send().await?
		.error_for_status()?;
	
	let reader = response
		.bytes_stream()
		.map_err(|e| io::Error::new(ErrorKind::Other, e))
		.into_async_read();

	let decoder = {
		let gz = GzipDecoder::new(BufReader::new(reader));
		BufReader::new(gz)
	};

	let mut lines = decoder.lines();
	let mut entries = Vec::new();

	while let Some(line) = lines.next().await {
		let line = line?;
		let parsed = serde_json::from_str(&line)?;
		
		entries.push(parsed);
	}

	Ok(entries)
}
