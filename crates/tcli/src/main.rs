use std::path::PathBuf;

use clap::Parser;
use cli::InitSubcommand;
use directories::BaseDirs;
use once_cell::sync::Lazy;
use package::resolver::PackageResolver;
use project::ProjectKind;
use wildmatch::WildMatch;

use crate::cli::{Args, Commands, ListSubcommand};
use crate::config::Vars;
use crate::error::Error;
use crate::game::registry::GameImportBuilder;
use crate::game::{ecosystem, registry};
use crate::project::manifest::ProjectManifest;
use crate::project::overrides::ProjectOverrides;
use crate::project::ProjectPath;
use crate::ui::reporter::IndicatifReporter;

mod cli;
mod config;
mod error;
mod game;
mod package;
mod project;
mod ts;
mod ui;
mod util;

pub static TCLI_HOME: Lazy<PathBuf> = Lazy::new(|| {
    let default_home = BaseDirs::new().unwrap().data_dir().join("tcli");

    Vars::HomeDir
        .into_var()
        .map_or_else(|_| default_home, PathBuf::from)
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
                &project_path,
                overwrite,
                ProjectKind::Dev(
                    ProjectOverrides::new()
                        .namespace_override(package_namespace)
                        .name_override(package_name)
                        .version_override(package_version),
                ),
            ),
            InitSubcommand::Profile => {
                project::create_new(&project_path, overwrite, ProjectKind::Profile)
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
            token = token.or_else(|| Vars::AuthKey.into_var().ok());
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

            let reporter = Box::new(IndicatifReporter);
            let project_path = ProjectPath::new(&project_path)?;

            let packages = PackageResolver::resolve_new(packages, &project_path).await?;
            packages.apply(reporter).await?;

            Ok(())
        }
        Commands::ImportGame {
            game_id,
            custom_id,
            custom_name,
            exe_path,
            tcli_directory,
            repository,
            project_path,
        } => {
            ts::init_repository("https://thunderstore.io", None);

            let project_path = ProjectPath::new(&project_path)?;

            GameImportBuilder::new(&game_id)
                .await?
                .with_custom_id(custom_id)
                .with_custom_name(custom_name)
                .with_custom_exe(exe_path)
                .import(&project_path)
        }
        Commands::UpdateSchema {} => {
            ts::init_repository("https://thunderstore.io", None);

            if !ecosystem::schema_exists() {
                let new = ecosystem::get_schema().await?;
                println!(
                    "Downloaded the latest ecosystem schema, version {}",
                    new.schema_version
                );

                return Ok(());
            }

            let current = ecosystem::get_schema().await?;
            ecosystem::remove_schema()?;
            let new = ecosystem::get_schema().await?;

            if current.schema_version == new.schema_version {
                println!(
                    "The local ecosystem schema is the latest, version {}",
                    new.schema_version
                );
            } else {
                println!(
                    "Updated ecosystem schema from version {} to {}",
                    current.schema_version, new.schema_version
                );
            }

            Ok(())
        }
        Commands::List { command } => match command {
            ListSubcommand::Platforms { target, detected } => {
                let platforms = registry::get_supported_platforms(&target);

                println!("TCLI supports the following platforms on {target}");
                for plat in platforms {
                    println!("- {plat}");
                }

                Ok(())
            }
            ListSubcommand::RegisteredGames { project_path } => {
                let project_path = ProjectPath::new(&project_path)?;
                let games = registry::get_registry(&project_path)?;

                for game in games {
                    println!("{game:#?}");
                }

                Ok(())
            }
            ListSubcommand::SupportedGames { search } => {
                let schema = ecosystem::get_schema().await?;
                let pattern = WildMatch::new(&search);

                let filtered = schema
                    .games
                    .iter()
                    .filter(|(_, game_def)| {
                        pattern.matches(&game_def.meta.display_name)
                            || pattern.matches(&game_def.label)
                    })
                    .collect::<Vec<_>>();

                for (_, game_def) in filtered.iter() {
                    println!("{}", game_def.meta.display_name);
                    println!("- label: {}", game_def.label);
                    println!("- uuid : {}", game_def.uuid);
                }

                let count = filtered.len();
                println!("\n{} games have been listed.", count);

                Ok(())
            }
        },
        _ => todo!("other commands"),
    }
}
