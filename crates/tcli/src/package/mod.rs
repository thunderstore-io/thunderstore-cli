mod cache;

use std::collections::HashMap;
use std::iter;
use std::path::PathBuf;
use std::sync::Arc;

use async_recursion::async_recursion;
use colored::Colorize;
use futures::future::try_join_all;
use futures::prelude::*;
use futures::stream::FuturesUnordered;
use tokio::sync::RwLock;

use crate::error::Error;
use crate::ts::experimental::package;
use crate::ts::package_reference::PackageReference;
use crate::ts::version::Version;

#[derive(Debug)]
pub enum PackageSource {
    Remote(String),
    Local(PathBuf),
}

#[derive(Debug)]
pub struct Package {
    pub identifier: PackageReference,
    pub source: PackageSource,
    pub dependencies: Vec<Package>,
}

impl Package {
    /// Resolve the package identifier from the remote repository / local cache, along with
    /// all specified dependencies.
    pub async fn resolve(ident: PackageReference) -> Result<Package, Error> {
        let final_deps = Arc::new(RwLock::new(HashMap::new()));
        let root_package = resolve_intern(ident.clone(), final_deps.clone())
            .await?
            .unwrap();

        print!(
            "{} resolved {}-{} ({}) ",
            "[✓]".green(),
            ident.namespace.bold(),
            ident.name.bold(),
            ident.version.to_string().truecolor(90, 90, 90),
        );

        if root_package.dependencies.len() == 1 {
            print!("and {} other dependent package", "1".green());
        } else {
            print!(
                "and {} other dependent packages",
                root_package.dependencies.len().to_string().green()
            );
        }

        println!();

        Ok(root_package)
    }

    pub async fn install(&self, project: PathBuf) {
        todo!()
    }
}

async fn package_already_resolved(
    ident: &PackageReference,
    final_deps: Arc<RwLock<HashMap<String, Version>>>,
) -> bool {
    let final_deps = final_deps.read().await;
    let loose_ident = ident.to_loose_ident_string();

    final_deps.contains_key(&loose_ident) && final_deps.get(&loose_ident).unwrap() >= &ident.version
}

#[async_recursion]
async fn resolve_intern(
    ident: PackageReference,
    final_deps: Arc<RwLock<HashMap<String, Version>>>,
) -> Result<Option<Package>, Error> {
    if package_already_resolved(&ident, final_deps.clone()).await {
        return Ok(None);
    }

    let package_data =
        package::get_version_metadata(&ident.namespace, &ident.name, ident.version).await?;

    let dep_tree = package_data
        .dependencies
        .into_iter()
        .filter(|x| !cache::package_exists(x).unwrap());

    // Determine the list of dependencies that also need to be resolved.
    let dep_tree = stream::iter(dep_tree)
        .filter_map(|x| async {
            let loose_ident = x.to_loose_ident_string();
            let final_deps = final_deps.read().await;

            if !final_deps.contains_key(&loose_ident)
                || final_deps.get(&loose_ident).unwrap() < &ident.version
            {
                Some(x)
            } else {
                None
            }
        })
        .map(|x| resolve_intern(x, final_deps.clone()))
        .collect::<FuturesUnordered<_>>();

    // Collect the result of the previous recursive resolution.
    let dep_tree = try_join_all(dep_tree.await)
        .await?
        .into_iter()
        .flatten()
        .collect::<Vec<_>>();

    // Add the resolved dependencies to the final deps tree, including the parent.
    let new_final_deps = dep_tree
        .iter()
        .map(|x| (x.identifier.to_loose_ident_string(), x.identifier.version))
        .chain(iter::once((ident.to_loose_ident_string(), ident.version)));

    final_deps.write().await.extend(new_final_deps);

    let package = Package {
        identifier: ident,
        source: PackageSource::Remote(package_data.download_url),
        dependencies: dep_tree,
    };

    Ok(Some(package))
}
