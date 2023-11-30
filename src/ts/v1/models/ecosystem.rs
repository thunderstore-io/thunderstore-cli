use std::collections::HashMap;

use serde::{Deserialize, Serialize};

use crate::ts::version::Version;

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct EcosystemSchema {
    pub schema_version: Version,
    pub games: HashMap<String, GameDef>,
    pub communities: HashMap<String, SchemaCommunity>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct GameDef {
    pub uuid: String,
    pub label: String,
    pub meta: GameDefMeta,
    pub distributions: Vec<GameDefPlatform>,
    pub r2modman: Option<GameDefR2MM>,
    pub thunderstore: Option<GameDefThunderstore>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct GameDefMeta {
    pub display_name: String,
    pub icon_url: Option<String>,
}

#[derive(Serialize, Deserialize, Debug, Clone, PartialEq)]
#[serde(tag = "platform")]
#[serde(rename_all = "kebab-case")]
pub enum GameDefPlatform {
    #[serde(rename = "egs")]
    EpicGames {
        identifier: String,
    },
    #[serde(rename = "xbox-game-pass")]
    GamePass {
        identifier: String,
    },
    Origin {
        identifier: String,
    },
    Steam {
        identifier: String,
    },
    SteamDirect {
        identifier: String,
    },
    Oculus,
    Other,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct GameDefR2MM {
    pub internal_folder_name: String,
    pub data_folder_name: String,
    pub settings_identifier: String,
    pub package_index: String,
    pub exclusions_url: String,
    pub steam_folder_name: String,
    pub exe_names: Vec<String>,
    pub game_instancetype: String,
    pub game_selection_display_mode: String,
    pub mod_loader_packages: Vec<R2MMModLoaderPackage>,
    pub install_rules: Vec<R2MMInstallRule>,
    pub relative_file_exclusions: Vec<String>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct R2MMModLoaderPackage {
    pub package_id: String,
    pub root_folder: String,
    pub loader: String,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct R2MMInstallRule {
    pub route: String,
    pub tracking_method: Option<String>,
    pub children: Option<Vec<R2MMInstallRule>>,
    pub default_file_extensions: Option<Vec<String>>,
    pub is_default_location: Option<bool>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct GameDefThunderstore {
    pub display_name: String,
    pub categories: HashMap<String, ThunderstoreCategory>,
    pub sections: HashMap<String, ThunderstoreSection>,
    pub discord_url: Option<String>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct ThunderstoreCategory {
    pub label: String,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
#[serde(rename_all = "camelCase")]
pub struct ThunderstoreSection {
    pub name: String,
    #[serde(default)]
    pub exclude_categories: Vec<String>,
    #[serde(default)]
    pub require_categories: Vec<String>,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct SchemaCommunity {
    pub display_name: String,
    pub categories: HashMap<String, CommunityCategory>,
    pub sections: HashMap<String, CommunitySection>,
    pub discord_url: Option<String>,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct CommunityCategory {
    pub label: String,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct CommunitySection {
    pub name: String,
    #[serde(default)]
    pub excluded_categories: Vec<String>,
    #[serde(default)]
    pub required_categories: Vec<String>,
}
