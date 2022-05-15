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
struct Args {
    #[clap(subcommand)]
    pub command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    Install {
        game_directory: PathBuf,
        bepinex_directory: PathBuf,
        zip_path: PathBuf,
    },
    Uninstall {
        game_directory: PathBuf,
        bepinex_directory: PathBuf,
        name: String,
    },
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
    #[error("Manifest does not contain namespace in manifest, which is required for mod installs")]
    MissingNamespace,
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
    let args = Args::parse();

    match args.command {
        Commands::Install {
            game_directory,
            bepinex_directory,
            zip_path,
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
            install(game_directory, bepinex_directory, zip_path)
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
    }
}

fn install(game_dir: PathBuf, bep_dir: PathBuf, zip_path: PathBuf) -> Result<()> {
    let mut zip = ZipArchive::new(std::fs::File::open(zip_path)?)?;

    if !zip.file_names().any(|name| name == "manifest.json") {
        bail!(Error::NoZipManifest);
    }

    let manifest_file = zip.by_name("manifest.json")?;

    let manifest: ManifestV1 =
        serde_json::from_reader(manifest_file).map_err(|_| Error::InvalidManifest)?;

    if manifest.name.starts_with("BepInExPack") {
        install_bepinex(game_dir, bep_dir, zip)
    } else {
        install_mod(bep_dir, zip, manifest)
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
            .or(Some("".to_string()))
            .unwrap()
            .starts_with("BepInExPack")
        {
            continue;
        }

        let dir_to_use = if filepath.ancestors().any(|part| {
            part.file_name()
                .or(Some(&OsString::new()))
                .unwrap()
                .to_string_lossy()
                == "BepInEx"
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
) -> Result<()> {
    let write_opts = OpenOptions::new().write(true).create(true).clone();

    let full_name = format!(
        "{}-{}",
        manifest.namespace.ok_or(Error::MissingNamespace)?,
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

        let out_path: PathBuf = if let Some(root) = top_level_directory_name(&filepath) {
            if let Some(remap) = remaps.get(&root as &str) {
                remap.join(remove_first_n_directories(&filepath, 1))
            } else {
                remaps["plugins"].join(filepath)
            }
        } else {
            remaps["plugins"].join(filepath)
        };

        let full_out_path = bep_dir.join(out_path);

        fs::create_dir_all(full_out_path.parent().unwrap())?;
        io::copy(&mut file, &mut write_opts.open(full_out_path)?)?;
    }

    Ok(())
}

fn uninstall(game_dir: PathBuf, bep_dir: PathBuf, name: String) -> Result<()> {
    todo!();
}

fn top_level_directory_name(path: &Path) -> Option<String> {
    path.ancestors()
        .skip(1)
        .filter(|x| !x.to_string_lossy().is_empty())
        .last()
        .map(|root| root.to_string_lossy().to_string())
}

// removes the first n directories from a path, eg a/b/c/d.txt with an n of 2 gives c/d.txt
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
