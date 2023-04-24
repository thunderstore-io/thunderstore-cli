use std::collections::HashMap;
use serde::{Serialize, Deserialize};

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct EcosystemSchema {
    schema_version: String,
    games: HashMap<String, GameDef>,
    communities: HashMap<String, SchemaCommunity>,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
pub struct GameDef {
    uuid: String,
    label: String,
    meta: GameDefMeta,
    distributions: Vec<GameDefPlatform>,
    r2modman: GameDefR2MM,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct GameDefMeta {
    display_name: String,
    icon_url: Option<String>,
}

#[derive(Serialize, Deserialize, Debug)]
struct GameDefPlatform {
    platform: String,
    identifier: Option<String>,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct GameDefR2MM {
    internal_folder_name: String,
    data_folder_name: String,
    settings_identifier: String,
    package_index: String,
    exclusions_url: String,
    steam_folder_name: String,
    exe_names: Vec<String>,
    game_instancetype: String,
    game_selection_display_mode: String,
    mod_loader_packages: Vec<R2MMModLoaderPackage>,
    install_rules: Vec<R2MMInstallRule>,
    relative_file_exclusions: Vec<String>,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct R2MMModLoaderPackage {
    package_id: String,
    root_folder: String,
    loader: String,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct R2MMInstallRule {
    route: String,
    tracking_method: Option<String>,
    children: Option<Vec<R2MMInstallRule>>,
    default_file_extensions: Option<Vec<String>>,
    is_default_location: Option<bool>,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct SchemaCommunity {
    display_name: String,
    categories: HashMap<String, CommunityCategory>,
    sections: HashMap<String, CommunitySection>,
    discord_url: String,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct CommunityCategory {
    label: String,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct CommunitySection {
    name: String,
    excluded_categories: Option<Vec<String>>,
    required_categories: Option<Vec<String>>,
}
