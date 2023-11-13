use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

use super::{ecosystem, steam};
use crate::error::Error;
use crate::game::win;
use crate::ts::v1::models::ecosystem::{GameDef, GameDefPlatform};
use crate::util::os::OS;

#[derive(Serialize, Deserialize, Debug, PartialEq)]
pub struct GameData {
    pub ecosystem_label: String,
    pub identifier: String,
    pub display_name: String,
    pub active_distribution: ActiveDistribution,
    pub possible_distributions: Vec<GameDefPlatform>,
}

#[derive(Serialize, Deserialize, Debug, PartialEq)]
pub struct ActiveDistribution {
    pub dist: GameDefPlatform,
    pub game_dir: PathBuf,
    pub data_dir: PathBuf,
    pub exe_path: PathBuf,
}

pub struct GameImportBuilder {
    game_def: GameDef,
    custom_id: Option<String>,
    custom_name: Option<String>,
    custom_exe: Option<PathBuf>,
}

impl GameImportBuilder {
    pub async fn new(game_id: &str) -> Result<Self, Error> {
        let game_def = ecosystem::get_schema()
            .await?
            .games
            .get(game_id)
            .ok_or_else(|| Error::InvalidGameId(game_id.into()))?
            .clone();

        Ok(GameImportBuilder {
            game_def,
            custom_id: None,
            custom_name: None,
            custom_exe: None,
        })
    }

    pub fn with_custom_id(self, custom_id: Option<String>) -> Self {
        GameImportBuilder { custom_id, ..self }
    }

    pub fn with_custom_name(self, custom_name: Option<String>) -> Self {
        GameImportBuilder {
            custom_name,
            ..self
        }
    }

    pub fn with_custom_exe(self, custom_exe: Option<PathBuf>) -> Self {
        GameImportBuilder { custom_exe, ..self }
    }

    /// Import the game as a new game definition, automatically determining the
    /// correct platform to use.
    ///
    /// Note that this function does not yet support Linux native Wine interop. The Windows native
    /// build of tcli must be run through Wine to detect games installed to the current prefix.
    pub fn import(self, game_registry: &Path) -> Result<(), Error> {
        let (dist, game_dir) = self
            .game_def
            .distributions
            .iter()
            .find_map(|dist| match dist {
                GameDefPlatform::Steam { identifier } => {
                    let id = identifier.parse::<u32>().unwrap();

                    steam::get_game_path(id).map(|x| (dist, x))
                }

                #[cfg(windows)]
                GameDefPlatform::GamePass { identifier } => {
                    win::gamepass::get_game_path(identifier).map(|x| (dist, x))
                }
                #[cfg(target_os = "linux")]
                GameDefPlatform::GamePass { _identifier } => None,

                #[cfg(windows)]
                GameDefPlatform::Origin { identifier } => {
                    win::eadesktop::get_game_path(identifier).map(|x| (dist, x))
                }
                #[cfg(target_os = "linux")]
                GameDefPlatform::Origin { _identifier } => None,

                #[cfg(windows)]
                GameDefPlatform::EpicGames { identifier } => {
                    win::egs::get_game_path(identifier).map(|x| (dist, x))
                }
                #[cfg(target_os = "linux")]
                GameDefPlatform::EpicGames { _identifier } => None,

                _ => None,
            })
            .unwrap();

        let r2modman = self.game_def.r2modman.unwrap();
        let game_dir = game_dir.canonicalize()?;
        let data_dir = game_dir.join(r2modman.data_folder_name);

        // TODO: Determine the path of the game's executable via the platform.
        let exe_path = self
            .custom_exe
            .unwrap_or_else(|| {
                r2modman
                    .exe_names
                    .iter()
                    .find_map(|x| {
                        let exe_path = game_dir.join(x);

                        if exe_path.exists() {
                            Some(exe_path)
                        } else {
                            None
                        }
                    })
                    .unwrap()
            })
            .canonicalize()?;

        let active_dist = ActiveDistribution {
            dist: dist.to_owned(),
            game_dir,
            data_dir,
            exe_path,
        };

        let data = GameData {
            identifier: self.custom_id.unwrap_or(self.game_def.label.clone()),
            ecosystem_label: self.game_def.label,
            display_name: self.custom_name.unwrap_or(self.game_def.meta.display_name),
            active_distribution: active_dist,
            possible_distributions: self.game_def.distributions,
        };

        write_data(game_registry, data)
    }

    pub fn as_steam(self) -> SteamImportBuilder {
        SteamImportBuilder {
            game_def: self.game_def,
        }
    }
}

pub struct SteamImportBuilder {
    game_def: GameDef,
}

impl SteamImportBuilder {
    pub fn with_steam_dir(self, steam_dir: &Path) -> Self {
        todo!()
    }

    /// Import the steam game as a new game definition.
    pub async fn import(self) -> Result<(), Error> {
        todo!()
    }
}

pub fn get_supported_platforms(target_os: &OS) -> Vec<&'static str> {
    let mut platforms = vec!["Steam", "DRM Free"];

    if matches!(target_os, OS::Windows) {
        platforms.extend(vec!["Epic Games Store (EGS)", "PC Game Pass", "EA Desktop"]);
    };

    platforms
}

pub fn get_registry(game_registry: &Path) -> Result<Vec<GameData>, Error> {
    let contents = fs::read_to_string(game_registry)?;

    Ok(serde_json::from_str(&contents)?)
}

pub fn get_game_data(game_registry: &Path, game_id: &str) -> Option<GameData> {
    let game_registry: Vec<GameData> = {
        let contents = fs::read_to_string(game_registry).ok()?;

        serde_json::from_str(&contents).ok()?
    };

    game_registry.into_iter().find(|x| x.identifier == game_id)
}

fn write_data(game_registry: &Path, data: GameData) -> Result<(), Error> {
    let mut file = OpenOptions::new()
        .create(true)
        .write(true)
        .open(&game_registry)?;

    let mut game_registry: Vec<GameData> = {
        let contents = fs::read_to_string(&game_registry)?;

        if contents.is_empty() {
            Vec::new()
        } else {
            serde_json::from_str(&contents).unwrap()
        }
    };

    if game_registry.contains(&data) {
        return Ok(());
    }

    game_registry.push(data);

    let data_json = serde_json::to_string_pretty(&game_registry).unwrap();
    file.write_all(data_json.as_bytes())?;

    Ok(())
}
