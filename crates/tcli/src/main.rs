mod ts;

use std::path::PathBuf;
use clap::{Parser, Subcommand};
use tokio::main;

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
struct Args {
    #[clap(subcommand)]
    commands: Commands,
}

#[derive(Subcommand, Debug)]
enum Commands {
    /// Initialize a new project configuration.
    Init {
        /// If present, overwrite current configuration.
        #[clap(long, default_value = "false")]
        overwrite: Option<bool>,

        /// Name for the package.
        #[clap(long)]
        package_name: Option<String>,

        /// Namespace for the package.
        #[clap(long)]
        package_namespace: Option<String>,

        /// Version number for the package.
        #[clap(long)]
        package_version: Option<String>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
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
        package_version: Option<String>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path for the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
    },

    /// Publish a package. By default this will also build a new package.
    Publish {
        /// If provided, use defined package instead of building.
        #[clap(long, value_parser)]
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
        package_version: Option<String>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path for the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
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
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
    },

    /// Uninstalls a mod from a profile.
    Uninstall {
        /// The identifier of the game to manage mods for.
        game_name: String,

        /// Path to a package .zip or package name in the format 'namespace-name(-version)'.
        package: String,

        /// Profile that the mod will be installed into.
        #[clap(long, default_value = "DefaultProfile")]
        profile: Option<String>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
    },

    /// Imports a new game for use by tcli.
    ImportGame {
        /// The identifier of the game to import.
        game_name: String,

        /// Path to the game executable to use when launching the game. Only works with servers.
        #[clap(long, value_parser)]
        exe_path: Option<PathBuf>,

        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
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
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,

        /// Arguments to run the game with. Takes precedence over --args.
        #[clap(last = true, name = "--")]
        trailing_args: Vec<String>,
    },

    /// List configured games, profiles, and mods.
    List {
        /// Directory where tcli keeps its data: %APPDATA%/ThunderstoreCLI on Windows and
        /// ~/.config/ThunderstoreCLI on Linux.
        #[clap(long, value_parser)]
        tcli_directory: Option<PathBuf>,

        /// URL of the default repository.
        #[clap(long)]
        repository: Option<String>,

        /// Path of the project configuration file.
        #[clap(long, value_parser, default_value = "./thunderstore.toml")]
        config_path: Option<PathBuf>,
    },
}

#[tokio::main]
async fn main() {
    use ts::package;
    let result = package::get_metadata("Mythic", "ServerLaunchFix").await;

    println!("{:#?}", result);

    let args = Args::parse();

    match args.commands {
        Commands::Run {
            game_name,
            profile,
            args,
            tcli_directory,
            repository,
            config_path,
            trailing_args,
        } => {
            println!("{:#?}", trailing_args);
        }
        _ => panic!("")
    }
}
