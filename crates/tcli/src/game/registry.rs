use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::rc::Rc;

use serde::{Deserialize, Serialize};

use super::{ecosystem, gamepass, steam};
use crate::error::Error;
use crate::ts::v1::models::ecosystem::{EcosystemSchema, GameDefPlatform};

#[derive(Serialize, Deserialize, Debug, PartialEq)]
pub struct GameData {
    pub ecosystem_label: String,
    pub identifier: String,
    pub display_name: String,
    pub root_dir: PathBuf,
    pub active_distribution: GameDefPlatform,
    pub possible_distributions: Vec<GameDefPlatform>,
}

pub struct GameImportBuilder {
    game_id: String,
    ecosystem: Rc<EcosystemSchema>,
}

impl GameImportBuilder {
    pub async fn new(game_id: &str) -> Result<Self, Error> {
        let ecosystem = Rc::new(ecosystem::get_schema().await?);

        Ok(GameImportBuilder {
            game_id: game_id.into(),
            ecosystem,
        })
    }

    /// Import the game as a new game definition, automatically determining the
    /// correct platform to use.
    pub fn import(self, project_dir: &Path) -> Result<(), Error> {
        let schema_entry = self.ecosystem.games.get(&self.game_id).unwrap();

        let (dist, game_dir) = schema_entry
            .distributions
            .iter()
            .find_map(|dist| match dist {
                GameDefPlatform::Steam { identifier } => {
                    let id = identifier.parse::<u32>().unwrap();

                    steam::get_game_path(id).map(|x| (dist, x))
                }
                GameDefPlatform::GamePass { identifier } => {
                    gamepass::get_game_path(&identifier).map(|x| (dist, x))
                }
                _ => None,
            })
            .unwrap();

        let data = GameData {
            ecosystem_label: schema_entry.label.clone(),
            identifier: schema_entry.label.clone(),
            display_name: schema_entry.meta.display_name.clone(),
            active_distribution: dist.to_owned(),
            root_dir: game_dir.canonicalize().unwrap(),
            possible_distributions: schema_entry.distributions.clone(),
        };

        write_data(project_dir, data)
    }

    pub fn as_steam(self) -> SteamImportBuilder {
        SteamImportBuilder {
            game_id: self.game_id,
            ecosystem: self.ecosystem,
        }
    }
}

pub struct SteamImportBuilder {
    game_id: String,
    ecosystem: Rc<EcosystemSchema>,
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

fn write_data(project_dir: &Path, data: GameData) -> Result<(), Error> {
    let game_registry = project_dir.join(".tcli/game_registry.json");

    let mut file = OpenOptions::new()
        .create(true)
        .write(true)
        .open(&game_registry)?;

    let mut game_registry: Vec<GameData> = {
        let contents = fs::read_to_string(&game_registry)?;

        if contents.len() == 0 {
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
    file.write_all(&data_json.as_bytes())?;

    Ok(())
}
