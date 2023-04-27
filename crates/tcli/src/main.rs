use std::path::PathBuf;

use clap::{Parser, Subcommand};

use crate::ts::version::Version;

pub mod error;
mod game;
mod project;
mod ts;

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
struct Args {
    #[clap(subcommand)]
    commands: Commands,
}

const DEFAULT_REPO: &str = "https://thunderstore.io";
const DEFAULT_MANIFEST: &str = "./thunderstore.toml";

#[derive(Subcommand, Debug)]
enum Commands {
    /// Initialize a new project configuration.
    Init {
        /// If present, overwrite current configuration.
        #[clap(long, default_value = "false")]
        overwrite: bool,

        /// Name for the package.
        #[clap(long)]
        package_name: Option<String>,

        /// Namespace for the package.
        #[clap(long)]
        package_namespace: Option<String>,

        /// Version number for the package.
        #[clap(long)]
        package_version: Option<Version>,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,
    },

    /// Build a package.
    Build {
        /// Name for the package.
        #[clap(long)]
        package_name: Option<String>,

        /// Namespace for the package.
        #[clap(long)]
        package_namespace: Option<String>,

        /// Version number for the package.
        #[clap(long)]
        package_version: Option<Version>,

        #[clap(long)]
        output_path: Option<PathBuf>,

        /// Path for the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,
    },

    /// Publish a package. By default this will also build a new package.
    Publish {
        /// If provided, use defined package instead of building.
        #[clap(long)]
        file: Option<PathBuf>,

        /// Authentication token to use when publishing the package.
        #[clap(long)]
        token: Option<String>,

        /// Name for the package.
        #[clap(long)]
        package_name: Option<String>,

        /// Namespace for the package.
        #[clap(long)]
        package_namespace: Option<String>,

        /// Version number for the package.
        #[clap(long)]
        package_version: Option<Version>,

        /// URL of the default repository.
        #[clap(long, default_value = DEFAULT_REPO)]
        repository: String,

        /// Path for the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,
    },

    /// Installs a mod into a profile.
    Install {
        /// The identifier of the game to manage mods for.
        game_name: String,

        /// Path to a package .zip or package name in the format 'namespace-name(-version)'.
        package: String,

        /// Profile that the mod will be installed into.
        #[clap(long, default_value = "DefaultProfile")]
        profile: Option<String>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long, default_value = DEFAULT_REPO)]
        repository: String,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,
    },

    /// Uninstalls a mod from a profile.
    Uninstall {
        /// The identifier of the game to manage mods for.
        game_name: String,

        /// Package name in the format 'namespace-name(-version)'.
        package: String,

        /// Profile that the mod will be installed into.
        #[clap(long, default_value = "DefaultProfile")]
        profile: String,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long, default_value = DEFAULT_REPO)]
        repository: String,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,
    },

    /// Imports a new game for use by tcli.
    ImportGame {
        /// The identifier of the game to import.
        game_name: String,

        /// Path to the game executable to use when launching the game. Only works with servers.
        #[clap(long)]
        exe_path: Option<PathBuf>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long, default_value = DEFAULT_REPO)]
        repository: String,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,
    },

    /// Run a game with mods.
    Run {
        /// The identifier of the game to run.
        game_name: String,

        /// Profile that the mod will be installed into.
        #[clap(long, default_value = "DefaultProfile")]
        profile: Option<String>,

        /// Arguments to run the game with. Anything after a trailing -- will be prioritized over this argument.
        #[clap(long)]
        args: Option<Vec<String>>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long, default_value = DEFAULT_REPO)]
        repository: String,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        config_path: PathBuf,

        /// Arguments to run the game with. Takes precedence over --args.
        #[clap(last = true, name = "--")]
        trailing_args: Vec<String>,
    },

    /// List configured games, profiles, and mods.
    List {
        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long)]
        tcli_directory: Option<PathBuf>,
    },
}

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
