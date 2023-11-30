use std::fs;
use std::path::PathBuf;
use std::str::FromStr;

use serde::{Deserialize, Serialize};
use winreg::enums::HKEY_LOCAL_MACHINE;
use winreg::RegKey;

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "PascalCase")]
struct PartialInstallManifest {
    install_location: PathBuf,
    app_name: String,
}

pub fn get_game_path(ident: &str) -> Option<PathBuf> {
    // There's a couple ways that we can retrieve the path of a game installed via EGS.
    // 1. Parse LauncherInstalled.dat in C:/ProgramData/Epic/UnrealEngineLauncher/
    // 2. Parse game manifest files in C:/ProgramData/Epic/EpicGamesLauncher/Data/Manifests
    // I'm going to go for the second option.

    // Attempt to get the path of the EGS /Data directory from the registry.
    let local = RegKey::predef(HKEY_LOCAL_MACHINE);
    let repo = local
        .open_subkey("Software\\WOW6432Node\\Epic Games\\EpicGamesLauncher")
        .ok()?;

    let manifests_dir = {
        let path: String = repo.get_value("AppDataPath").ok()?;

        PathBuf::from_str(&path)
    }
    .ok()?
    .join("Manifests");

    if !manifests_dir.exists() {
        return None;
    }

    // Manifest files are JSON files with .item extensions.
    let manifest_files = fs::read_dir(manifests_dir)
        .ok()?
        .filter_map(|x| x.ok())
        .map(|x| x.path())
        .filter(|x| x.is_file() && x.extension().is_some())
        .filter(|x| x.extension().unwrap() == "item")
        .collect::<Vec<_>>();

    // Search for the manifest which contains the correct game AppName.
    let game_path = manifest_files.into_iter().find_map(|x| {
        let file_contents = fs::read_to_string(x).unwrap();
        let manifest: PartialInstallManifest = serde_json::from_str(&file_contents).unwrap();

        if manifest.app_name == ident {
            Some(manifest.install_location)
        } else {
            None
        }
    })?;

    if !game_path.is_dir() {
        return None;
    }

    Some(game_path.canonicalize().unwrap())
}
