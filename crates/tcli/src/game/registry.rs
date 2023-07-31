use std::path::PathBuf;

use itertools::Itertools;
use serde::{Deserialize, Serialize};

use super::{gamepass, steam};
use crate::ts::v1::models::ecosystem::{EcosystemSchema, GameDefPlatform};

#[derive(Deserialize, Serialize, Debug)]
pub struct GameData {
    pub ecosystem_label: String,
    pub identifier: String,
    pub display_name: String,
    /// This will not be filled in until it is needed to avoid prompting the user for all games
    pub active_distribution: Option<ActivePlatform>,
    pub possible_distributions: Vec<GameDefPlatform>,
}

#[derive(Deserialize, Serialize, Debug)]
pub enum ActivePlatform {
    None { path: PathBuf },
    Steam { id: u32 },
    GamePass { id: String },
    Oculus { path: PathBuf },
    // TODO: actually write in the others
    Other,
}

impl ActivePlatform {
    pub fn get_game_dir(&self) -> Option<PathBuf> {
        match self {
            ActivePlatform::None { path } => Some(path.to_owned()),
            ActivePlatform::Steam { id } => steam::get_game_path(*id),
            ActivePlatform::GamePass { id } => gamepass::get_game_path(id).unwrap(),
            _ => None,
        }
    }
}

pub fn create_from_schema(schema: &EcosystemSchema) -> Vec<GameData> {
    schema
        .games
        .values()
        .map(|game| GameData {
            ecosystem_label: game.label.clone(),
            identifier: game.label.clone(),
            display_name: game.meta.display_name.clone(),
            active_distribution: None,
            possible_distributions: game.distributions.clone(),
        })
        .collect_vec()
}
