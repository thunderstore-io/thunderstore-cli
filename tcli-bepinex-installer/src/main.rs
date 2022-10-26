use std::{
    collections::HashMap,
    ffi::OsString,
    fs::{self, OpenOptions},
    io::{self, Read, Seek},
    path::{Path, PathBuf},
};

use anyhow::{bail, Result};
use clap::{Parser, Subcommand};
use serde::Deserialize;
use zip::ZipArchive;

#[derive(Parser)]
#[clap(author, version, about)]
struct ClapArgs {
    #[clap(subcommand)]
    pub command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    Install {
        game_directory: PathBuf,
        bepinex_directory: PathBuf,
        zip_path: PathBuf,
        #[arg(long)]
        namespace_backup: Option<String>,
    },
    Uninstall {
        game_directory: PathBuf,
        bepinex_directory: PathBuf,
        name: String,
    },
    StartInstructions {
        game_directory: PathBuf,
        bepinex_directory: PathBuf,
        #[arg(long)]
        game_platform: Option<String>,
    }
}

#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("Path does not exist: {0}")]
    PathDoesNotExist(PathBuf),
    #[error("ZIP file does not contain a Thunderstore manifest")]
    NoZipManifest,
    #[error("Invalid manifest in ZIP")]
    InvalidManifest,
    #[error("Malformed zip")]
    MalformedZip,
    #[error("Manifest does not contain a namespace and no backup was given, namespaces are required for mod installs")]
    MissingNamespace,
    #[error("Mod name is invalid (eg doesn't use a - between namespace and name)")]
    InvalidModName,
}

#[derive(Deserialize)]
#[allow(unused)]
struct ManifestV1 {
    pub namespace: Option<String>,
    pub name: String,
    pub description: String,
    pub version_number: String,
    pub dependencies: Vec<String>,
    pub website_url: String,
}

fn main() -> Result<()> {
    let args = ClapArgs::parse();

    match args.command {
        Commands::Install {
            game_directory,
            bepinex_directory,
            zip_path,
            namespace_backup,
        } => {
            if !game_directory.exists() {
                bail!(Error::PathDoesNotExist(game_directory));
            }
            if !bepinex_directory.exists() {
                bail!(Error::PathDoesNotExist(bepinex_directory));
            }
            if !zip_path.exists() {
                bail!(Error::PathDoesNotExist(zip_path));
            }
            install(game_directory, bepinex_directory, zip_path, namespace_backup)
        }
        Commands::Uninstall {
            game_directory,
            bepinex_directory,
            name,
        } => {
            if !game_directory.exists() {
                bail!(Error::PathDoesNotExist(game_directory));
            }
            if !bepinex_directory.exists() {
                bail!(Error::PathDoesNotExist(bepinex_directory));
            }
            uninstall(game_directory, bepinex_directory, name)
        }
        Commands::StartInstructions {
            bepinex_directory,
            game_platform,
            ..
        } => {
            output_instructions(bepinex_directory, game_platform);
            Ok(())
        }
    }
}

fn install(game_dir: PathBuf, bep_dir: PathBuf, zip_path: PathBuf, namespace_backup: Option<String>) -> Result<()> {
    let mut zip = ZipArchive::new(std::fs::File::open(zip_path)?)?;

    if !zip.file_names().any(|name| name == "manifest.json") {
        bail!(Error::NoZipManifest);
    }

    let manifest_file = zip.by_name("manifest.json")?;

    let manifest: ManifestV1 =
        serde_json::from_reader(manifest_file).map_err(|_| Error::InvalidManifest)?;

    if manifest.name.starts_with("BepInEx") {
        install_bepinex(game_dir, bep_dir, zip)
    } else {
        install_mod(bep_dir, zip, manifest, namespace_backup)
    }
}

fn install_bepinex(
    game_dir: PathBuf,
    bep_dir: PathBuf,
    mut zip: ZipArchive<impl Read + Seek>,
) -> Result<()> {
    let write_opts = OpenOptions::new().write(true).create(true).clone();

    for i in 0..zip.len() {
        let mut file = zip.by_index(i)?;

        if file.is_dir() {
            continue;
        }

        let filepath = file.enclosed_name().ok_or(Error::MalformedZip)?.to_owned();

        if !top_level_directory_name(&filepath)
            .unwrap_or_else(|| "".to_string())
            .starts_with("BepInExPack")
        {
            continue;
        }

        let dir_to_use = if filepath.ancestors().any(|part| {
            part.file_name()
                .unwrap_or(&OsString::new())
                .to_string_lossy() == "BepInEx"
        }) {
            &bep_dir
        } else {
            &game_dir
        };

        // this removes the BepInExPack*/ from the path
        let resolved_path = remove_first_n_directories(&filepath, 1);

        fs::create_dir_all(dir_to_use.join(resolved_path.parent().unwrap()))?;
        io::copy(
            &mut file,
            &mut write_opts.open(dir_to_use.join(resolved_path))?,
        )?;
    }

    Ok(())
}

fn install_mod(
    bep_dir: PathBuf,
    mut zip: ZipArchive<impl Read + Seek>,
    manifest: ManifestV1,
    namespace_backup: Option<String>,
) -> Result<()> {
    let write_opts = OpenOptions::new().write(true).create(true).clone();

    let full_name = format!(
        "{}-{}",
        manifest.namespace.or(namespace_backup).ok_or(Error::MissingNamespace)?,
        manifest.name
    );

    let mut remaps: HashMap<&str, PathBuf> = HashMap::new();
    remaps.insert(
        "plugins",
        Path::new("BepInEx").join("plugins").join(&full_name),
    );
    remaps.insert(
        "patchers",
        Path::new("BepInEx").join("patchers").join(&full_name),
    );
    remaps.insert(
        "monomod",
        Path::new("BepInEx").join("monomod").join(&full_name),
    );
    remaps.insert("config", Path::new("BepInEx").join("config"));

    for i in 0..zip.len() {
        let mut file = zip.by_index(i)?;

        if file.is_dir() {
            continue;
        }

        let filepath = file.enclosed_name().ok_or(Error::MalformedZip)?.to_owned();

        let out_path: PathBuf = if let Some((root, count)) = search_for_directory(&filepath, &["plugins", "patchers", "monomod", "config"]) {
            if let Some(remap) = remaps.get(root) {
                remap.join(remove_first_n_directories(&filepath, count))
            } else {
                remaps["plugins"].join(filepath.file_name().unwrap())
            }
        } else {
            remaps["plugins"].join(filepath.file_name().unwrap())
        };

        let full_out_path = bep_dir.join(out_path);

        fs::create_dir_all(full_out_path.parent().unwrap())?;
        io::copy(&mut file, &mut write_opts.open(full_out_path)?)?;
    }

    Ok(())
}

fn uninstall(game_dir: PathBuf, bep_dir: PathBuf, name: String) -> Result<()> {
    if name.split_once('-').ok_or(Error::InvalidModName)?.1.starts_with("BepInExPack") {
        uninstall_bepinex(game_dir, bep_dir)
    } else {
        uninstall_mod(bep_dir, name)
    }
}

fn uninstall_bepinex(game_dir: PathBuf, bep_dir: PathBuf) -> Result<()> {
    delete_file_if_not_deleted(game_dir.join("winhttp.dll"))?;
    delete_file_if_not_deleted(game_dir.join("doorstop_config.ini"))?;
    delete_file_if_not_deleted(game_dir.join("run_bepinex.sh"))?;
    delete_dir_if_not_deleted(game_dir.join("doorstop_libs"))?;
    delete_dir_if_not_deleted(bep_dir.join("BepInEx"))?;

    Ok(())
}

fn uninstall_mod(bep_dir: PathBuf, name: String) -> Result<()> {
    let actual_bep = bep_dir.join("BepInEx");
    delete_dir_if_not_deleted(actual_bep.join("plugins").join(&name))?;
    delete_dir_if_not_deleted(actual_bep.join("patchers").join(&name))?;
    delete_dir_if_not_deleted(actual_bep.join("monomod").join(&name))?;

    Ok(())
}

fn output_instructions(bep_dir: PathBuf, platform: Option<String>) {
    if platform.as_ref().map(|p| p == "windows").unwrap_or(true) {
        let drive_prefix = match platform {
            Some(_) => "Z:",
            None => ""
        };

        println!("ARGUMENTS:--doorstop-enable true --doorstop-target {}{}", drive_prefix, bep_dir.join("BepInEx").join("core").join("BepInEx.Preloader.dll").to_string_lossy().replace('/', "\\"));
        println!("WINEDLLOVERRIDE:winhttp")
    } else {
        eprintln!("native linux not implmented");
    }
}

fn top_level_directory_name(path: &Path) -> Option<String> {
    path.ancestors()
        .skip(1)
        .filter(|x| !x.to_string_lossy().is_empty())
        .last()
        .map(|root| root.to_string_lossy().to_string())
}

fn search_for_directory<'a>(path: &Path, targets: &[&'a str]) -> Option<(&'a str, usize)> {
    let mut path_parts = path
        .ancestors()
        .filter(|x| !x.to_string_lossy().is_empty())
        .map(|x| x.file_name().unwrap())
        .collect::<Vec<_>>();
    path_parts.reverse();
    for (index, part) in path_parts.into_iter().enumerate() {
        for target in targets {
            if part.to_string_lossy() == *target {
                return Some((target, index + 1));
            }
        }
    }
    None
}

/// removes the first n directories from a path, eg a/b/c/d.txt with an n of 2 gives c/d.txt
fn remove_first_n_directories(path: &Path, n: usize) -> PathBuf {
    PathBuf::from_iter(
        path.ancestors()
            .collect::<Vec<_>>()
            .into_iter()
            .rev()
            .filter(|x| !x.to_string_lossy().is_empty())
            .skip(n)
            .map(|part| part.file_name().unwrap()),
    )
}

fn delete_file_if_not_deleted<T: AsRef<Path>>(path: T) -> io::Result<()> {
    match fs::remove_file(path) {
        Ok(_) => Ok(()),
        Err(e) => match e.kind() {
            io::ErrorKind::NotFound => Ok(()),
            _ => Err(e),
        },
    }
}

fn delete_dir_if_not_deleted<T: AsRef<Path>>(path: T) -> io::Result<()> {
    match fs::remove_dir_all(path) {
        Ok(_) => Ok(()),
        Err(e) => match e.kind() {
            io::ErrorKind::NotFound => Ok(()),
            _ => Err(e),
        },
    }
}
