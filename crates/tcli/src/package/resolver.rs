use std::collections::{HashMap, VecDeque};

use futures_util::future::try_join_all;
use petgraph::prelude::NodeIndex;
use petgraph::{Directed, Graph};

use super::Package;
use crate::error::Error;
use crate::package::index::PackageIndex;
use crate::project::lock::LockFile;
use crate::project::ProjectPath;
use crate::ts::package_reference::PackageReference;
use crate::ui::reporter::Reporter;

pub struct PackageResolver {
    pub packages_to_install: Vec<Package>,
    lockfile: LockFile,
    project: ProjectPath,
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
        project: &ProjectPath,
    ) -> Result<Self, Error> {
        let package_index = PackageIndex::open().await?;

        let start = std::time::Instant::now();

        let mut graph = Graph::<&PackageReference, (), Directed>::new();
        let mut graph_lookup: HashMap<String, NodeIndex> = HashMap::new();

        let mut iter_queue: VecDeque<&PackageReference> = VecDeque::from(packages.iter().collect::<Vec<_>>());

        while let Some(package_ident) = iter_queue.pop_front() {
            let package = package_index.get_package(package_ident).unwrap();
            let loose_ident = package_ident.to_loose_ident_string();

            // If the graph DOES NOT contain the identifier, add it.
            let node_index = *graph_lookup
                .entry(loose_ident.clone())
                .or_insert_with(|| graph.add_node(package_ident));

            let graph_package_ident = graph[node_index];

            // If the graph's package is of a lesser version, replace it with the new package ident.
            if graph_package_ident.version < package.version {
                println!("\treplacing {} with {}", graph_package_ident, package_ident);
                graph[node_index] = package_ident;
            }

            for dependency in package.dependencies.iter() {
                let cntrpart_index = graph_lookup.get(&dependency.to_loose_ident_string());

                // Queue up this dependency for processing if:
                // 1. This dependency already exists within the graph, but is a lesser version.
                // 2. This dependency does not exist within the graph.
                if matches!(cntrpart_index, Some(index) if graph[*index].version < dependency.version) || cntrpart_index.is_none() {
                    iter_queue.push_back(dependency);
                }

                // Get the already existing dependency's node to the graph, adding it if it DNE.
                let dep_index = *graph_lookup
                    .entry(dependency.to_loose_ident_string())
                    .or_insert_with(|| graph.add_node(dependency));

                graph.add_edge(node_index, dep_index, ());
            }

            println!("visited package {:#}", package_ident);
        }

        let topo_packages = petgraph::algo::toposort(&graph, None).unwrap();
        let stop = std::time::Instant::now();
        let pkg_count = topo_packages.len();

        println!("{} packages in {}ms", pkg_count, (stop - start).as_millis());

        let packages_to_install = try_join_all(topo_packages
            .iter()
            .rev()
            .map(|x| async {
                let ident = graph[*x];
                Package::resolve_new(ident).await
            })).await?;

        let lockfile = LockFile::open_or_new(&project.path().join("Thunderstore.lock"))?;

        Ok(PackageResolver {
            packages_to_install,
            lockfile,
            project: project.clone(),
        })
    }

    /// Apply the newly resolved packages onto the previously specified project.
    pub async fn apply(mut self, reporter: Box<dyn Reporter>) -> Result<(), Error> {
        let multi = reporter.create_progress();
        let jobs = self.packages_to_install
            .iter()
            .map(|package| async {
                package.add(&self.project, multi.add_bar()).await
            });

        try_join_all(jobs).await?;

        self.lockfile.merge(&self.packages_to_install);
        self.lockfile.commit()?;

        Ok(())
    }
}
