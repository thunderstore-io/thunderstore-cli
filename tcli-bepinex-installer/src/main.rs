use std::{
    collections::HashMap,
    env,
    ffi::OsString,
    fs::{self, OpenOptions},
    io::{self, Read, Seek},
    path::{Path, PathBuf},
};

use anyhow::{bail, Result};
use clap::{Parser, Subcommand, ValueEnum};
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
        game_platform: GamePlatform,
    },
}

#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("Path does not exist: {0}")]
    PathDoesNotExist(PathBuf),
    #[error("ZIP file does not contain a Thunderstore manifest")]
    NoZipManifest,
    #[error("Invalid manifest in ZIP, serde_json error: {0}")]
    InvalidManifest(serde_json::Error),
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
    #[serde(default)]
    pub dependencies: Vec<String>,
    pub website_url: String,
}

#[derive(Debug, Deserialize, Clone, Copy, ValueEnum)]
enum GamePlatform {
    Windows,
    Proton,
    Linux,
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
            install(
                game_directory,
                bepinex_directory,
                zip_path,
                namespace_backup,
            )
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
            game_directory,
            bepinex_directory,
            game_platform,
            ..
        } => {
            output_instructions(game_directory, bepinex_directory, game_platform);
            Ok(())
        }
    }
}

fn install(
    game_dir: PathBuf,
    bep_dir: PathBuf,
    zip_path: PathBuf,
    namespace_backup: Option<String>,
) -> Result<()> {
    let mut zip = ZipArchive::new(std::fs::File::open(zip_path)?)?;

    if !zip.file_names().any(|name| name == "manifest.json") {
        bail!(Error::NoZipManifest);
    }

    let mut manifest_file = zip.by_name("manifest.json")?;

    let mut manifest_text = String::new();
    manifest_file.read_to_string(&mut manifest_text).unwrap();
    if manifest_text.starts_with('\u{FEFF}') {
        manifest_text.remove(0);
    }

    drop(manifest_file);

    let manifest: ManifestV1 =
        serde_json::from_str(&manifest_text).map_err(Error::InvalidManifest)?;

    if manifest.name.starts_with("BepInEx") && zip.file_names().any(|f| f.ends_with("winhttp.dll"))
    {
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
    let write_opts = {
        let mut opts = OpenOptions::new();
        opts.write(true).create(true);
        opts
    };

    for i in 0..zip.len() {
        let mut file = zip.by_index(i)?;

        if file.is_dir() {
            continue;
        }

        let filepath = file.enclosed_name().ok_or(Error::MalformedZip)?.to_owned();

        if !top_level_directory_name(&filepath)
            .unwrap_or("")
            .starts_with("BepInExPack")
        {
            continue;
        }

        let in_bep_folder = filepath.ancestors().any(|part| {
            part.file_name()
                .unwrap_or(&OsString::new())
                .to_string_lossy()
                == "BepInEx"
        });

        let dir_to_use = if in_bep_folder { &bep_dir } else { &game_dir };

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
        manifest
            .namespace
            .or(namespace_backup)
            .ok_or(Error::MissingNamespace)?,
        manifest.name
    );

    let mut remaps = HashMap::new();
    remaps.insert(
        Path::new("BepInEx").join("plugins"),
        Path::new("BepInEx").join("plugins").join(&full_name),
    );
    remaps.insert(
        Path::new("BepInEx").join("patchers"),
        Path::new("BepInEx").join("patchers").join(&full_name),
    );
    remaps.insert(
        Path::new("BepInEx").join("monomod"),
        Path::new("BepInEx").join("monomod").join(&full_name),
    );
    remaps.insert(
        Path::new("BepInEx").join("config"),
        Path::new("BepInEx").join("config"),
    );

    let default_remap = &remaps[&Path::new("BepInEx").join("plugins")];

    for i in 0..zip.len() {
        let mut file = zip.by_index(i)?;

        if file.is_dir() {
            continue;
        }

        let filepath = file.enclosed_name().ok_or(Error::MalformedZip)?.to_owned();
        let filename = match filepath.file_name() {
            Some(name) => name,
            None => continue,
        };

        let mut out_path = None;
        'outer: for remap in remaps.keys() {
            for variant in get_path_variants(remap) {
                if let Ok(p) = filepath.strip_prefix(variant) {
                    out_path = Some(remaps[remap].join(p));
                    break 'outer;
                }
            }
        }
        if out_path.is_none() {
            out_path = Some(default_remap.join(filename));
        }

        let full_out_path = bep_dir.join(out_path.unwrap());

        fs::create_dir_all(full_out_path.parent().unwrap())?;
        io::copy(&mut file, &mut write_opts.open(full_out_path)?)?;
    }

    Ok(())
}

fn uninstall(game_dir: PathBuf, bep_dir: PathBuf, name: String) -> Result<()> {
    if name
        .split_once('-')
        .ok_or(Error::InvalidModName)?
        .1
        .starts_with("BepInEx")
    {
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

fn output_instructions(game_dir: PathBuf, bep_dir: PathBuf, platform: GamePlatform) {
    let drive_prefix = if matches!(platform, GamePlatform::Proton) {
        "Z:\\"
    } else {
        ""
    };

    let bep_preloader_dll = bep_dir
        .join("BepInEx")
        .join("core")
        .join("BepInEx.Preloader.dll")
        .to_string_lossy()
        .into_owned();

    match platform {
        GamePlatform::Windows | GamePlatform::Proton => {
            println!(
                "ARGUMENTS:--doorstop-enable true --doorstop-target {}{}",
                drive_prefix,
                bep_preloader_dll.replace('/', "\\")
            );
            println!("WINEDLLOVERRIDE:winhttp");
        }
        GamePlatform::Linux => {
            println!("ENVIRONMENT:DOORSTOP_ENABLE=TRUE");
            println!("ENVIRONMENT:DOORSTOP_INVOKE_DLL_PATH={}", bep_preloader_dll);
            println!(
                "ENVIRONMENT:DOORSTOP_CORLIB_OVERRIDE_PATH={}",
                game_dir.join("unstripped_corlib").to_string_lossy()
            );

            let mut ld_library = OsString::from(game_dir.join("doorstop_libs"));
            if let Some(orig) = env::var_os("LD_LIBRARY_PATH") {
                ld_library.push(":");
                ld_library.push(orig);
            }

            println!(
                "ENVIRONMENT:LD_LIBRARY_PATH={}",
                ld_library.to_string_lossy()
            );

            let mut ld_preload = OsString::from({
                // FIXME: properly determine arch of the game exe, instead of assuming its the same as this exe
                if cfg!(target_arch = "x86_64") {
                    "libdoorstop_x64.so"
                } else {
                    "libdoorstop_x86.so"
                }
            });
            if let Some(orig) = env::var_os("LD_PRELOAD") {
                ld_preload.push(":");
                ld_preload.push(orig);
            }

            println!("ENVIRONMENT:LD_PRELOAD={}", ld_preload.to_string_lossy());
        }
    }
}

fn top_level_directory_name(path: &Path) -> Option<&str> {
    path.components()
        .next()
        .and_then(|n| n.as_os_str().to_str())
}

/// removes the first n directories from a path, eg a/b/c/d.txt with an n of 2 gives c/d.txt
fn remove_first_n_directories(path: &Path, n: usize) -> PathBuf {
    PathBuf::from_iter(path.iter().skip(n))
}

fn get_path_variants(path: &Path) -> Vec<PathBuf> {
    let mut res = vec![path.into()];
    let components: Vec<_> = path.components().collect();
    for i in 1usize..components.len() {
        res.push(PathBuf::from_iter(components.iter().skip(i)))
    }
    res
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
