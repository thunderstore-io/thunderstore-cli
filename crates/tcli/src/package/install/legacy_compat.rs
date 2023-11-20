use std::str::FromStr;

use crate::package::Package;
use crate::ts::package_reference::PackageReference;

/// Attempt to determine the valid installer for the given package by searching through
/// the resolved package dependency list.
///
/// Note: This is not a "correct" solution as it does not explicitly search for *dependencies*
/// of the given package. This works well enough though, given that we don't support multi
/// mod loader installs.
pub fn determine_installer_from_resolved(packages: &[PackageReference]) -> Option<PackageReference> {    
    for package in packages {
        if package.name.to_lowercase() == "bepinex" {
            return Some(PackageReference::from_str("0x7FF-BepInExInstaller-0.0.1").unwrap())
        }
    }

    None
}

pub fn determine_installer_from_contents(package: &Package) -> Option<PackageReference> {
    todo!()
}
