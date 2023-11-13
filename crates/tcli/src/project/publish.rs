use std::path::PathBuf;

use crate::error::Error;
use crate::project::manifest::ProjectManifest;
use crate::ts::experimental::models::publish::PackageSubmissionMetadata;
use crate::ts::experimental::publish;

pub async fn publish(
    manifest: &ProjectManifest,
    archive_path: PathBuf,
) -> Result<(), Error> {
    let package = manifest
        .package
        .as_ref()
        .ok_or(Error::MissingTable("package"))?;

    if !archive_path.is_file() {
        Err(Error::FileNotFound(archive_path.clone()))?;
    }

    let usermedia = publish::upload_file(archive_path).await?;
    publish::package_submit(&PackageSubmissionMetadata {
        author_name: package.namespace.to_string(),
        communities: manifest
            .publish
            .iter()
            .map(|p| p.community.clone())
            .collect(),
        has_nsfw_content: package.contains_nsfw_content,
        community_categories: manifest
            .publish
            .iter()
            .map(|p| (p.community.clone(), p.categories.clone()))
            .collect(),
        upload_uuid: usermedia.uuid,
    })
    .await?;

    Ok(())
}
