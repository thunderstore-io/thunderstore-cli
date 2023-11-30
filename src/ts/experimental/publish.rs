use std::io::{Read, Seek, SeekFrom};
#[cfg(target_os = "windows")]
use std::os::windows::prelude::OpenOptionsExt;
use std::path::Path;

use base64::prelude::BASE64_STANDARD;
use base64::Engine;
use indicatif::ProgressBar;
use md5::digest::FixedOutput;
use md5::Md5;
use reqwest::{header, Body};
use tokio::io::{AsyncReadExt, AsyncSeekExt};

use crate::error::{Error, IoResultToTcli, ReqwestToTcli};
use crate::ts::experimental::models::publish::*;
use crate::ts::{AUTH, CLIENT, EX};
use crate::ui::PROGRESS_STYLE;

pub async fn usermedia_initiate(
    params: &UserMediaInitiateUploadParams,
) -> Result<UserMediaInitiateUploadResponse, Error> {
    Ok(CLIENT
        .post(format!("{EX}/usermedia/initiate-upload/"))
        .header(
            header::AUTHORIZATION,
            AUTH.get().ok_or(Error::MissingAuthToken)?,
        )
        .json(params)
        .send()
        .await?
        .error_for_status_tcli()
        .await?
        .json()
        .await?)
}

pub async fn usermedia_finish(
    uuid: String,
    params: &UserMediaFinishUploadParams,
) -> Result<(), Error> {
    CLIENT
        .post(format!("{EX}/usermedia/{uuid}/finish-upload/"))
        .header(
            header::AUTHORIZATION,
            AUTH.get().ok_or(Error::MissingAuthToken)?,
        )
        .json(params)
        .send()
        .await?
        .error_for_status_tcli()
        .await?;
    Ok(())
}

pub async fn usermedia_abort(uuid: String) -> Result<(), Error> {
    CLIENT
        .post(format!("{EX}/usermedia/{uuid}/abort-upload/"))
        .header(
            header::AUTHORIZATION,
            AUTH.get().ok_or(Error::MissingAuthToken)?,
        )
        .send()
        .await?
        .error_for_status_tcli()
        .await?;
    Ok(())
}

pub async fn upload_file(path: impl AsRef<Path>) -> Result<UserMedia, Error> {
    let path = path.as_ref();
    let open_options = &{
        let mut opts = std::fs::OpenOptions::new();
        opts.read(true);
        // don't want people modifying the file as we read, can only be done on windows sadly
        #[cfg(target_os = "windows")]
        opts.share_mode(1);
        opts
    };
    let mut file = tokio::fs::OpenOptions::from(open_options.clone())
        .open(path)
        .await?;
    let length = file.seek(SeekFrom::End(0)).await?;
    // the file isn't used again past here, but we need it kept open anyway so it isn't modified

    let initiate_response = usermedia_initiate(&UserMediaInitiateUploadParams {
        filename: path.file_name().unwrap().to_string_lossy().into_owned(),
        file_size_bytes: length,
    })
    .await?;

    let usermedia = initiate_response.user_media;

    println!(
        "Uploading in {} chunks...",
        initiate_response.upload_urls.len()
    );

    let progress_bar = &ProgressBar::new(length).with_style(PROGRESS_STYLE.clone());

    let tags_result: Result<Vec<CompletedPart>, Error> =
        futures::future::try_join_all(initiate_response.upload_urls.into_iter().map(
            |url| async move {
                let mut file = open_options.open(path).map_fs_error(path)?;
                file.seek(SeekFrom::Start(url.offset))?;

                let mut md5 = Md5::default();
                std::io::copy(&mut file.try_clone().unwrap().take(url.length), &mut md5)?;
                let md5 = BASE64_STANDARD.encode(md5.finalize_fixed());

                file.seek(SeekFrom::Start(url.offset))?;

                let data_stream = tokio::fs::File::from_std(file).take(url.length);
                let with_progress = progress_bar.wrap_async_read(data_stream);

                let upload_response = CLIENT
                    .put(url.url)
                    .header(header::CONTENT_LENGTH, url.length)
                    .header("Content-MD5", md5)
                    .body(Body::wrap_stream(tokio_util::io::ReaderStream::new(
                        with_progress,
                    )))
                    .send()
                    .await?
                    .error_for_status_tcli()
                    .await?;
                let etag = upload_response
                    .headers()
                    .get("ETag")
                    .expect("Expected ETag in upload response")
                    .to_str()
                    .expect("ETag was not a valid string");

                Ok(CompletedPart {
                    etag: etag.to_string(),
                    part_number: url.part_number,
                })
            },
        ))
        .await;

    progress_bar.finish();

    let parts = match tags_result {
        Ok(parts) => parts,
        Err(e) => {
            usermedia_abort(usermedia.uuid)
                .await
                .expect("Failed to abort usermedia upload upon upload failure");
            return Err(e);
        }
    };

    usermedia_finish(
        usermedia.uuid.clone(),
        &UserMediaFinishUploadParams { parts },
    )
    .await?;

    // explicit drop here to make sure it lasts to here
    drop(file);
    Ok(usermedia)
}

pub async fn package_submit(
    params: &PackageSubmissionMetadata,
) -> Result<PackageSubmissionResult, Error> {
    Ok(CLIENT
        .post(format!("{EX}/submission/submit/"))
        .header(
            header::AUTHORIZATION,
            AUTH.get().ok_or(Error::MissingAuthToken)?,
        )
        .json(params)
        .send()
        .await?
        .error_for_status_tcli()
        .await?
        .json()
        .await?)
}
