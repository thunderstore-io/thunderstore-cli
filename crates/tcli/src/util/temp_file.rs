#![allow(dead_code)]

use std::{
    mem::ManuallyDrop,
    path::{Path, PathBuf},
};

use crate::error::{Error, IoResultToTcli};

pub struct TempFile<F>(Option<PathBuf>, ManuallyDrop<F>);

impl<F> TempFile<F> {
    pub fn file(&self) -> &F {
        &self.1
    }

    pub fn file_mut(&mut self) -> &mut F {
        &mut self.1
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
        Ok(Self(Some(path.into()), ManuallyDrop::new(file)))
    }

    pub fn into_async(mut self) -> TempFile<tokio::fs::File> {
        TempFile(
            self.0.take(),
            ManuallyDrop::new(tokio::fs::File::from_std(self.1.try_clone().unwrap())),
        )
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
        Ok(Self(Some(path.into()), ManuallyDrop::new(file)))
    }

    pub async fn into_std(mut self) -> TempFile<std::fs::File> {
        // get the new instance, drop the first, then convert to std (if done like to_async, this would panic)
        let path = self.0.take();

        let new_async = self.1.try_clone().await.unwrap();

        drop(self);

        TempFile(path, ManuallyDrop::new(new_async.into_std().await))
    }
}

impl<F> Drop for TempFile<F> {
    fn drop(&mut self) {
        // SAFETY: This is in the drop implementation, we just need this to be dropped before we delete
        unsafe { ManuallyDrop::drop(&mut self.1) }
        if let Some(path) = &self.0 {
            std::fs::remove_file(path).expect("Failed to remove temporary file.");
        }
    }
}
