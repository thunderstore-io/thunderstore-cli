#![allow(unused)]

use std::collections::HashMap;
use std::str::FromStr;

use once_cell::sync::OnceCell;

use crate::error::Error;
use crate::game::ecosystem;
use crate::package::resolver::DependencyGraph;
use crate::ts::package_reference::PackageReference;

/// This is a map which binds modloader packages to mod installer packages.
static INSTALLER_MAP: OnceCell<HashMap<String, String>> = OnceCell::new();

/// Attempt to determine the valid installer for the given package by searching through
/// the resolved package dependency list.
///
/// Note: This returns a *loose* package identifier, it is up to the callee to resolve
/// into a versioned package identifier.
pub async fn installer_from_graph(
    target: &PackageReference,
    graph: &DependencyGraph,
) -> Result<Option<String>, Error> {
    let installer_map = match INSTALLER_MAP.get() {
        Some(x) => x,
        None => {
            let installer_map = init_installer_map().await?;
            INSTALLER_MAP.get_or_init(|| installer_map)
        }
    };

    let dependencies = graph.get_dependencies(target);
    if dependencies.is_none() {
        return Ok(None);
    }

    let dependencies = dependencies.unwrap();
    let loader = dependencies
        .into_iter()
        .find_map(|package| installer_map.get(&package.to_loose_ident_string()))
        .map_or(None, |x| Some(x.clone()));

    Ok(loader)
}

async fn init_installer_map() -> Result<HashMap<String, String>, Error> {
    let schema = ecosystem::get_schema().await?;
    let map: HashMap<_, _> = HashMap::from_iter(vec![
        ("bepinex", "metherul-BepInEx_Installer"),
        ("melonloader", "metherul-MelonLoader_Installer"),
        ("northstar", "metherul-Northstar_Installer"),
    ]);

    let installer_map = schema
        .games
        .into_iter()
        .filter_map(|(_, def)| def.r2modman)
        .map(|def| def.mod_loader_packages)
        .flatten()
        .filter_map(|loader| match map.get(loader.loader.as_str()) {
            Some(installer) => Some((loader.package_id, installer.to_string())),
            None => None,
        })
        .collect::<HashMap<_, _>>();

    Ok(installer_map)
}

#[cfg(test)]
mod tests {
    use std::sync::Once;

    use super::*;
    use crate::package::resolver;

    static INIT: Once = Once::new();

    fn init() {
        INIT.call_once(|| {
            crate::ts::init_repository("https://thunderstore.io", None);
        })
    }

    #[tokio::test]
    async fn test_legacy_installer_for() -> Result<(), Error> {
        init();

        // TODO: Update this (and the other test) with non RoR2 packages when the new
        // package index API is complete.
        let cases = vec![
            (
                "tristanmcpherson-R2API-5.0.5",
                Some("metherul-BepInEx_Installer".to_string()),
            ),
            (
                "TeamMoonstorm-Starstorm2-0.5.5",
                Some("metherul-BepInEx_Installer".to_string()),
            ),
            (
                "bbepis-BepInExPack-5.4.2113",
                Some("metherul-BepInEx_Installer".to_string()),
            ),
            // ("gnonme-ModThatIsNotMod-0.3.6", Some("metherul-MelonLoader_Installer".to_string())),
        ];

        for (target, expected) in cases.into_iter() {
            let target = PackageReference::from_str(target).unwrap();

            let graph = resolver::resolve_packages(vec![target.clone()]).await?;
            let result = installer_from_graph(&target, &graph).await?;

            assert_eq!(result, expected);
        }

        Ok(())
    }
}
