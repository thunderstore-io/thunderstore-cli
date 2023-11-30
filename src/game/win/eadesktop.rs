use std::path::PathBuf;

use winreg::enums::HKEY_LOCAL_MACHINE;
use winreg::RegKey;

pub fn get_game_path(ident: &str) -> Option<PathBuf> {
    // Looks like Origin isn't functional anymore, so lets go ahead and just detect the EA launcher.
    // TODO: Needs legitimate implementation.

    let local = RegKey::predef(HKEY_LOCAL_MACHINE);

    let subkey = format!("Software\\{}\\", ident.replace('.', "\\"));
    let repo = local.open_subkey(subkey).ok()?;

    repo.get_value("Install Dir").ok().map(|x: String| x.into())
}
