use std::path::PathBuf;

use winreg::enums::HKEY_LOCAL_MACHINE;
use winreg::RegKey;

pub fn get_game_path(id: &str) -> Option<PathBuf> {
    let local = RegKey::predef(HKEY_LOCAL_MACHINE);
    let repo = local
        .open_subkey("Software\\Microsoft\\GamingServices\\PackageRepository")
        .ok()?;

    let game_uuid = repo
        .open_subkey("Package\\")
        .ok()?
        .enum_values()
        .find(|x| {
            let x = x.as_ref().unwrap();

            x.0.starts_with(id)
        })?
        .ok()?
        .1
        .to_string()
        .replace('\"', "");

    let game_path: String = {
        let game_local = repo.open_subkey(format!("Root\\{}", game_uuid)).ok()?;
        let subkey = game_local.enum_keys().next()?.ok()?;

        game_local
            .open_subkey(subkey)
            .ok()?
            .get_value("Root")
            .ok()?
    };

    Some(game_path.into())
}
