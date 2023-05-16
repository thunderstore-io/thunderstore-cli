use clap::Parser;

use crate::cli::{Args, Commands};

mod cli;
mod error;
mod game;
mod project;
mod ts;

#[tokio::main]
async fn main() -> Result<(), crate::error::Error> {
    // let result = package::get_metadata("Mythic", "ServerLaunchFix").await;
    // let result = package::get_all().await;
    // let result = ecosystem::get_schema().await.unwrap();

    match Args::parse().commands {
        Commands::Init {
            overwrite,
            package_namespace,
            package_name,
            package_version,
            config_path,
        } => project::create_new(
            config_path,
            overwrite,
            package_namespace,
            package_name,
            package_version,
        ),
        Commands::Build {
            package_name,
            package_namespace,
            package_version,
            output_path,
            config_path,
        } => project::build(
            config_path,
            output_path,
            package_namespace,
            package_name,
            package_version,
        ),
        _ => todo!("other commands"),
    }
}
