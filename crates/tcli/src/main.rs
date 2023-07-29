use std::env;
use std::path::PathBuf;

use clap::Parser;
use cli::InitSubcommand;
use config::Config;
use directories::BaseDirs;
use once_cell::sync::Lazy;
use package::resolver::PackageResolver;
use project::ProjectKind;

use crate::cli::{Args, Commands};
<<<<<<< Updated upstream
=======
use crate::config::Vars;
>>>>>>> Stashed changes
use crate::error::Error;
use crate::project::manifest::ProjectManifest;
use crate::project::overrides::ProjectOverrides;
use crate::ui::reporter::IndicatifReporter;

mod cli;
mod error;
mod game;
mod package;
mod project;
mod ts;
mod ui;
mod util;

pub static TCLI_HOME: Lazy<PathBuf> = Lazy::new(|| {
    let default_home = BaseDirs::new().unwrap().data_dir().join("tcli");

<<<<<<< Updated upstream
    env::var("TCLI_HOME").map_or_else(|_| default_home, PathBuf::from)
=======
    Vars::HomeDir
        .into_var()
        .map_or_else(|_| default_home, PathBuf::from)
>>>>>>> Stashed changes
});

#[tokio::main]
async fn main() -> Result<(), Error> {
    match Args::parse().commands {
        Commands::Init {
            command,
            overwrite,
            project_path,
        } => match command {
            InitSubcommand::Project {
                package_name,
                package_namespace,
                package_version,
            } => project::create_new(
                project_path,
                overwrite,
                ProjectKind::Dev(
                    ProjectOverrides::new()
                        .namespace_override(package_namespace)
                        .name_override(package_name)
                        .version_override(package_version),
                ),
            ),
            InitSubcommand::Profile => {
                project::create_new(project_path, overwrite, ProjectKind::Profile)
            }
        },
        Commands::Build {
            package_name,
            package_namespace,
            package_version,
            output_dir,
            project_path,
        } => {
            let mut manifest = ProjectManifest::read_from_file(project_path)?;
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
        Commands::Add {
            packages,
            project_path,
        } => {
            ts::init_repository("https://thunderstore.io", None);

            let config = Config::load(project_path.parent().unwrap()).unwrap();
            dbg!(config);

            panic!("");

            let reporter = Box::new(IndicatifReporter);

            let packages = PackageResolver::resolve_new(packages, project_path).await?;
            packages.apply(reporter).await?;

            Ok(())
        }
        _ => todo!("other commands"),
    }
}
