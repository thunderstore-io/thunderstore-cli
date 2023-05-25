use clap::Parser;

use crate::cli::{Args, Commands};
use crate::error::Error;
use crate::project::manifest::ProjectManifest;
use crate::project::overrides::ProjectOverrides;

mod cli;
mod error;
mod game;
mod project;
mod ts;
mod ui;

#[tokio::main]
async fn main() -> Result<(), Error> {
    match Args::parse().commands {
        Commands::Init {
            overwrite,
            package_namespace,
            package_name,
            package_version,
            project_path,
        } => project::create_new(
            project_path,
            overwrite,
            ProjectOverrides::new()
                .namespace_override(package_namespace)
                .name_override(package_name)
                .version_override(package_version),
        ),
        Commands::Build {
            package_name,
            package_namespace,
            package_version,
            output_dir,
            project_path,
        } => {
            let mut manifest = ProjectManifest::read_from_file(&project_path)?;
            manifest.apply_overrides(
                ProjectOverrides::new()
                    .namespace_override(package_namespace)
                    .name_override(package_name)
                    .version_override(package_version)
                    .output_dir_override(output_dir),
            )?;
            project::build(&manifest)?;
            Ok(())
        }
        Commands::Publish {
            file,
            mut token,
            package_name,
            package_namespace,
            package_version,
            repository,
            project_path,
        } => {
            token = token.or_else(|| std::env::var("TCLI_AUTH_TOKEN").ok());
            if token.is_none() {
                return Err(Error::MissingAuthToken);
            }
            let mut manifest = ProjectManifest::read_from_file(&project_path)?;
            manifest.apply_overrides(
                ProjectOverrides::new()
                    .namespace_override(package_namespace)
                    .name_override(package_name)
                    .version_override(package_version)
                    .repository_override(repository),
            )?;
            ts::init_repository(
                manifest
                    .config
                    .repository
                    .as_deref()
                    .ok_or(Error::MissingRepository)?,
                token.as_deref(),
            );
            project::publish(&manifest, file).await
        }
        _ => todo!("other commands"),
    }
}
