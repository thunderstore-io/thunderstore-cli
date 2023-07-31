use std::path::PathBuf;

use winreg::enums::HKEY_LOCAL_MACHINE;
use winreg::RegKey;

use crate::error::Error;

pub fn get_game_path(id: &str) -> Result<Option<PathBuf>, Error> {
    let local = RegKey::predef(HKEY_LOCAL_MACHINE);
    let repo = local.open_subkey("Software\\Microsoft\\GamingServices\\PackageRepository")?;

    let game_uuid = repo
        .open_subkey("Package\\")?
        .enum_values()
        .find(|x| {
            let x = x.as_ref().unwrap();

            x.0.starts_with(id)
        })
        .unwrap()?
        .1
        .to_string()
        .replace("\"", "");

    let game_path: String = {
        let game_local = repo.open_subkey(format!("Root\\{}", game_uuid)).unwrap();
        let subkey = game_local.enum_keys().next().unwrap()?;

        game_local.open_subkey(subkey)?.get_value("Root")?
    };

    Ok(Some(game_path.into()))
}
