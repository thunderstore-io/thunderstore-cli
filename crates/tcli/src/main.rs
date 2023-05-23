use std::path::Path;

use clap::Parser;

use crate::cli::{Args, Commands};
use crate::project::manifest::ProjectManifest;
use crate::project::overrides::ProjectOverrides;

mod cli;
mod error;
mod game;
mod project;
mod ts;

#[tokio::main]
async fn main() -> Result<(), error::Error> {
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
            project::build(project_path.parent().unwrap_or(Path::new("./")), manifest)
        }
        _ => todo!("other commands"),
    }
}
