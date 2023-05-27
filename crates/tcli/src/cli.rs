use std::path::PathBuf;

use clap::{Parser, Subcommand};

use crate::ts::package_reference::PackageReference;
use crate::ts::version::Version;

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
pub struct Args {
    #[clap(subcommand)]
    pub commands: Commands,
}

const DEFAULT_MANIFEST: &str = "./thunderstore.toml";

#[derive(Subcommand, Debug)]
pub enum Commands {
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
        project_path: PathBuf,
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
        output_dir: Option<PathBuf>,

        /// Path for the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        project_path: PathBuf,
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
        #[clap(long)]
        repository: Option<String>,

        /// Path for the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        project_path: PathBuf,
    },

    /// Installs a mod into a profile.
    Add {
        /// Path to a package .zip or package name in the format 'namespace-name(-version)'.
        package: PackageReference,
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
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        project_path: PathBuf,
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
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        project_path: PathBuf,
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
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        project_path: PathBuf,

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
