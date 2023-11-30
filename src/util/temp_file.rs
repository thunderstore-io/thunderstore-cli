#![allow(dead_code)]

use std::path::{Path, PathBuf};

use crate::error::{Error, IoResultToTcli};

pub struct TempFile<F>(Option<PathBuf>, Option<F>);

impl<F> TempFile<F> {
    pub fn file(&self) -> &F {
        self.1.as_ref().unwrap()
    }

    pub fn file_mut(&mut self) -> &mut F {
        self.1.as_mut().unwrap()
    }
}

impl TempFile<std::fs::File> {
    pub fn open_std(path: impl AsRef<Path>) -> Result<Self, Error> {
        let path = path.as_ref();
        let file = std::fs::OpenOptions::new()
            .create_new(true)
            .read(true)
            .write(true)
            .open(path)
            .map_fs_error(path)?;
        Ok(Self(Some(path.into()), Some(file)))
    }

    pub fn into_async(mut self) -> TempFile<tokio::fs::File> {
        TempFile(self.0.take(), self.1.take().map(tokio::fs::File::from_std))
    }
}

impl TempFile<tokio::fs::File> {
    pub async fn open_async(path: impl AsRef<Path>) -> Result<Self, Error> {
        let path = path.as_ref();
        let file = tokio::fs::OpenOptions::new()
            .create_new(true)
            .read(true)
            .write(true)
            .open(path)
            .await
            .map_fs_error(path)?;
        Ok(Self(Some(path.into()), Some(file)))
    }

    pub async fn into_std(mut self) -> TempFile<std::fs::File> {
        TempFile(self.0.take(), Some(self.1.take().unwrap().into_std().await))
    }
}

impl<F> Drop for TempFile<F> {
    fn drop(&mut self) {
        // We need this file handle to be dropped before we delete
        drop(self.1.take());
        if let Some(path) = &self.0 {
            std::fs::remove_file(path).expect("Failed to remove temporary file.");
        }
    }
}
