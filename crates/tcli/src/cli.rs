use std::path::PathBuf;

use clap::{Parser, Subcommand};

use crate::ts::package_reference::PackageReference;
use crate::ts::version::Version;
use crate::util::os::OS;

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
pub struct Args {
    #[clap(subcommand)]
    pub commands: Commands,
}

const DEFAULT_MANIFEST: &str = "./Thunderstore.toml";

#[derive(Subcommand, Debug, Clone)]
pub enum InitSubcommand {
    /// Creates a tcli project which can be used to build and publish a package.
    Project {
        /// Name for the package.
        #[clap(long)]
        package_name: Option<String>,

        /// Namespace for the package.
        #[clap(long)]
        package_namespace: Option<String>,

        /// Version number for the package.
        #[clap(long)]
        package_version: Option<Version>,
    },
    /// Creates a tcli profile which is used to build and run mod installations.
    Profile,
}

#[derive(Subcommand, Debug, Clone)]
pub enum ListSubcommand {
    /// List the platforms tcli supports.
    Platforms {
        /// List platforms which tcli has *explicit* support for on the specified OS.
        /// "windows", "linux", or "macos" are valid.
        #[clap(long, default_value = std::env::consts::OS)]
        target: OS,

        /// List platforms detected as installed on this machine.
        #[clap(long, default_value = "false")]
        detected: bool,
    },
    /// List imported games and their metadata.
    ImportedGames {
        #[clap(long, default_value = DEFAULT_MANIFEST)]
        project_path: PathBuf,
    },
    /// List supported games and their metadata.
    SupportedGames {
        /// The search pattern that will be used to query and filter games listed from the schema.
        /// This pattern is tested against the game's display name AND id.
        #[clap(default_value = "*")]
        search: String,
    },
}

#[derive(Subcommand, Debug)]
pub enum Commands {
    /// Initialize a new project configuration.
    Init {
        #[clap(subcommand)]
        command: InitSubcommand,

        /// If present, overwrite current configuration.
        #[clap(long, default_value = "false")]
        overwrite: bool,

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

    /// Adds a mod to a project.
    Add {
        /// Path to a package .zip or package name in the format 'namespace-name(-version)'.
        packages: Vec<PackageReference>,

        /// Path of the project configuration file.
        #[clap(long, default_value = "./")]
        project_path: PathBuf,
    },

    /// Removes a mod from the project.
    Remove {
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
        ///
        /// Use the `list` command to query the list of imported and supported games.
        game_id: String,

        #[clap(long)]
        /// The custom identifier this game will be referenced by.
        custom_id: Option<String>,

        #[clap(long)]
        /// The custom name this game will be displayed as.
        custom_name: Option<String>,

        /// Path to the game executable to use when launching the game. Only works with servers.
        #[clap(long)]
        exe_path: Option<PathBuf>,

        /// Directory where tcli keeps its data.
        /// %APPDATA%/Roaming/tcli on Windows, ~/.config/tcli on Linux.
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
        ///
        /// Use the `list` command to query the list of imported and supported games.
        game_name: String,

        /// Arguments to run the game with. Anything after a trailing -- will be prioritized over this argument.
        #[clap(long)]
        args: Option<Vec<String>>,

        /// Directory where tcli keeps its data:
        /// %APPDATA%/Roaming/tcli on Windows and ~/.config/tcli on Linux.
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
        #[clap(subcommand)]
        command: ListSubcommand,
    },

    /// Update the tcli ecosystem schema.
    UpdateSchema,
}
