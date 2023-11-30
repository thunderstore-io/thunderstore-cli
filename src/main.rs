use std::path::PathBuf;

use clap::Parser;
use cli::InitSubcommand;
use colored::Colorize;
use directories::BaseDirs;
use once_cell::sync::Lazy;
use project::ProjectKind;
use wildmatch::WildMatch;

use crate::cli::{Args, Commands, ListSubcommand};
use crate::config::Vars;
use crate::error::Error;
use crate::game::registry::GameImportBuilder;
use crate::game::{ecosystem, registry};
use crate::package::install::Installer;
use crate::project::lock::LockFile;
use crate::project::overrides::ProjectOverrides;
use crate::project::Project;
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
        } => {
            match command {
                Some(InitSubcommand::Project {
                    package_name,
                    package_namespace,
                    package_version,
                }) => {
                    let overrides = ProjectOverrides::new()
                        .namespace_override(package_namespace)
                        .name_override(package_name)
                        .version_override(package_version);

                    Project::create_new(&project_path, overwrite, ProjectKind::Dev(overrides))?;
                }

                Some(InitSubcommand::Profile) | None => {
                    Project::create_new(&project_path, overwrite, ProjectKind::Profile)?;
                }
            }

            Ok(())
        },
        Commands::Build {
            package_name,
            package_namespace,
            package_version,
            output_dir,
            project_path,
        } => {
            let project = Project::open(&project_path)?;
            let overrides = ProjectOverrides::new()
                .namespace_override(package_namespace)
                .name_override(package_name)
                .version_override(package_version)
                .output_dir_override(output_dir);
            
            project.build(overrides)?;
            Ok(())
        }
        Commands::Publish {
            package_archive,
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
            
            let project = Project::open(&project_path)?;
            let manifest = project.get_manifest()?;
            
            ts::init_repository(
                manifest
                    .config
                    .repository
                    .as_deref()
                    .ok_or(Error::MissingRepository)?,
                token.as_deref(),
            );

            let archive_path = match package_archive {
                Some(x) if x.is_file() => Ok(x),
                Some(x) => Err(Error::FileNotFound(x)),
                None => {
                    let overrides = ProjectOverrides::new()
                        .namespace_override(package_namespace)
                        .name_override(package_name)
                        .version_override(package_version)
                        .repository_override(repository);
            
                    project.build(overrides)
                }
            }?;

            project::publish(&manifest, archive_path).await
        }
        Commands::Add {
            packages,
            project_path,
        } => {
            ts::init_repository("https://thunderstore.io", None);

            let reporter = Box::new(IndicatifReporter);

            let project = Project::open(&project_path)?;
            project.add_packages(&packages[..])?;
            project.commit(reporter).await?;

            return Ok(());

            let lockfile = LockFile::open_or_new(&project_path).unwrap();
            let installer_package = lockfile
                .packages
                .get("TestInstaller-Metherul-0.1.0")
                .unwrap();

            println!("{:?}", installer_package);

            let installer = Installer::load_and_prepare(&installer_package).await?;

            println!("{:?}", installer.exec_path);

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

            let project = Project::open(&project_path)?;

            GameImportBuilder::new(&game_id)
                .await?
                .with_custom_id(custom_id)
                .with_custom_name(custom_name)
                .with_custom_exe(exe_path)
                .import(&project.game_registry_path)
        }
        
        Commands::Run { 
            game_id, 
            vanilla, 
            args, 
            tcli_directory, 
            repository, 
            project_path, 
            trailing_args
        } => {
            let project = Project::open(&project_path)?;
            let args = args.unwrap_or(vec![])
                .into_iter()
                .chain(trailing_args.into_iter())
                .collect::<Vec<_>>();
            
            project.start_game(
                &game_id,
                !vanilla,
                args,
            ).await?;

            Ok(())
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
            ListSubcommand::ImportedGames { project_path } => {
                let project = Project::open(&project_path)?;
                let games = registry::get_registry(&project.game_registry_path)?;

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
            ListSubcommand::InstalledMods { project_path } => {
                let project = Project::open(&project_path)?;
                let lock = LockFile::open_or_new(&project.lockfile_path)?;

                println!("Installed packages:");

                for (_, package) in lock.packages {
                    println!(
                        "- {}-{} ({})",
                        package.identifier.namespace.bold(),
                        package.identifier.name.bold(),
                        package.identifier.version.to_string().truecolor(90, 90, 90)
                    );
                }

                Ok(())
            }
        },
        _ => todo!("other commands"),
    }
}

