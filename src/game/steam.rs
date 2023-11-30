use std::path::PathBuf;

use steamlocate::SteamDir;

pub fn get_game_path(id: u32) -> Option<PathBuf> {
    let mut steam = SteamDir::locate()?;
    let app = steam.app(&id)?;

    Some(app.path.to_owned())
}
