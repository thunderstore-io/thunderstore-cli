use std::collections::{HashMap, VecDeque};
use std::path::PathBuf;

use futures_util::future::try_join_all;

use super::Package;
use crate::error::Error;
use crate::ts::package_reference::PackageReference;
use crate::ui::reporter::Reporter;

pub struct PackageResolver {
    pub packages_to_install: Vec<Package>,
    project: PathBuf,
}

impl PackageResolver {
    /// Generate a deduplicated list of package dependencies. This describes every package that
    /// needs to be downloaded and installed for all of the root packages to function.
    ///
    /// This takes into account:
    /// 1. Packages already installed into the project.
    /// 2. Dependencies specified within local packages within the cache.
    /// 3. Dependencies specified within the remote repository.
    pub async fn resolve_new(
        packages: Vec<PackageReference>,
        project: PathBuf,
    ) -> Result<Self, Error> {
        let mut dep_map: HashMap<String, Package> = HashMap::new();
        let mut queue: VecDeque<PackageReference> = VecDeque::from(packages.clone());

        // Generate top-level package dependencies first. We then iterate down through the tree
        // until all have been resolved.
        while let Some(package_ident) = queue.pop_front() {
            let package = Package::resolve_new(package_ident).await?;
            let package_deps = package
                .dependencies
                .iter()
                // Filter out packages which have already been resolved or are of lower version.
                .filter(|dep| {
                    let loose_ident = dep.to_loose_ident_string();

                    !dep_map.contains_key(&loose_ident)
                        || dep_map.get(&loose_ident).unwrap().identifier.version < dep.version
                })
                .map(|dep| async {
                    let loose_ident = dep.to_loose_ident_string();
                    let dep_package = Package::resolve_new(dep.clone()).await?;

                    Ok::<(String, Package), Error>((loose_ident, dep_package))
                });

            let package_deps = try_join_all(package_deps).await?;

            dep_map.extend(package_deps);
            dep_map.insert(package.identifier.to_loose_ident_string(), package);
        }

        let packages_to_install = dep_map.into_values().collect::<Vec<_>>();

        Ok(PackageResolver {
            packages_to_install,
            project,
        })
    }

    /// Apply the newly resolved packages onto the previously specified project.
    pub async fn apply(
        &self,
        reporter: Box<dyn Reporter>,
    ) -> Result<(), Error> {
        let project_path = self.project.as_path();

        let multi = reporter.create_progress();

        let jobs = self
            .packages_to_install
            .iter()
            .map(|package| package.add(project_path, multi.add_bar()));

        try_join_all(jobs).await?;

        Ok(())
    }
}
