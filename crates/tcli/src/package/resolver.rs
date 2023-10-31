use std::collections::{HashMap, HashSet, VecDeque};
use std::iter;
use std::rc::Rc;

use futures_util::future::try_join_all;
use petgraph::data::Build;
use petgraph::dot::{Config, Dot};
use petgraph::graphmap::NodeTrait;
use petgraph::prelude::{GraphMap, NodeIndex};
use petgraph::{Directed, Graph};

use super::Package;
use crate::error::Error;
use crate::package::index::PackageIndex;
use crate::project::lock::LockFile;
use crate::project::ProjectPath;
use crate::ts::package_reference::PackageReference;
use crate::ui::reporter::Reporter;

struct PackageGraphEntry<'a> {
    package: &'a mut Package,
    index: NodeIndex,
}

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
                let lgc_index = graph_lookup.get(&dependency.to_loose_ident_string());

                // Queue up if (a) the lgc does not exist or (b) the lgc's version is < this dep's version.
                match lgc_index {
                    Some(index) if graph[*index].version < dependency.version => {
                        iter_queue.push_back(dependency);
                    },
                    Some(_) => (),
                    None => iter_queue.push_back(dependency),
                };

                let dep_index = graph.add_node(dependency);
                let dep_loose_ident = dependency.to_loose_ident_string();

                if !graph_lookup.contains_key(&dep_loose_ident) {
                    iter_queue.push_back(dependency);
                }

                graph.add_edge(node_index, dep_index, ());
            }

            println!("visited package {:#}", package_ident);
        }

        panic!("");


        let mut graph = Graph::<Rc<Package>, ()>::new();
        let mut graph_map: HashMap<String, NodeIndex> = HashMap::new();

        let mut search_queue: VecDeque<(NodeIndex, Rc<Package>)> = VecDeque::new();
        let mut already_resolved: HashSet<String> = HashSet::new();

        // Start by resolving the given packages and adding them to the graph and search queue.
        for package in packages.clone() {
            let resolved = Rc::new(Package::resolve_new(package).await?);
            let loose_ident = resolved.identifier.to_loose_ident_string();

            let graph_index = graph.add_node(resolved.clone());

            graph_map.insert(loose_ident, graph_index);
            search_queue.push_back((graph_index, resolved));
        }

        // Iterate the search queue until nothing is left, adding resolved dependencies where needed.
        while let Some((package_index, package)) = search_queue.pop_front() {
            let package_refs = package
                .dependencies
                .iter()
                .filter(|dep_package| !already_resolved.contains(&dep_package.to_string()));
            // .map(|dep| async {
            //     let loose_ident = dep.to_loose_ident_string();
            //     let graph_index = match graph_map.get(&loose_ident) {
            //         Some(graph_index) => *graph_index,
            //         None => {
            //             let package = Rc::new(Package::resolve_new(dep.clone()).await?);
            //             graph.add_node(package)
            //         }
            //     };

            //     Ok::<(NodeIndex, &Rc<Package>), Error>((graph_index, &graph[graph_index]))
            // })).await?;

            for package_ref in package_refs {
                // Handle multi-version dependencies.
                if let Some(test) = graph_map.get(&package_ref.to_loose_ident_string()) {
                    let test_package = &graph[*test];
                }

                // Determine the node index of this package by either getting it from the graph map
                // or by resolving a new package, adding it to the graph, and then using the resulting index.
                let node_index = match graph_map.get(&package_ref.to_loose_ident_string()) {
                    Some(index) => *index,
                    None => {
                        let package = Rc::new(Package::resolve_new(package_ref.clone()).await?);
                        graph.add_node(package)
                    }
                };

                // Connect the parent package to the new node index.
                graph.add_edge(package_index, node_index, ());
                search_queue.push_back((node_index, graph[node_index].clone()));
            }

            already_resolved.insert(package.identifier.to_string());

            println!("resolved: {}", package.identifier.to_string());
        }

        println!("{:?}", Dot::with_config(&graph, &[Config::EdgeNoLabel]));

        let topo_thing = petgraph::algo::toposort(&graph, None).unwrap();
        for thing in topo_thing {
            println!(
                "[{}] {}",
                thing.index(),
                graph[thing].identifier.to_string()
            );
        }

        panic!("");

        let mut dep_map: HashMap<String, Package> = HashMap::new();
        let mut queue: VecDeque<PackageReference> = VecDeque::from(packages.clone());

        let mut deps = Graph::<String, ()>::new();

        let mut test: HashMap<String, NodeIndex> = HashMap::new();

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
            let mut add_node = |value: &Package, deps: &mut Graph<String, ()>| {
                if let Some(idx) = test.get(&value.identifier.to_loose_ident_string()) {
                    *idx
                } else {
                    let idx = deps.add_node(value.identifier.to_loose_ident_string());
                    test.insert(value.identifier.to_loose_ident_string(), idx);
                    idx
                }
            };

            // If the parent node already exists within the graph hashmap thingie, use the cached index instead.
            let parent_idx = add_node(&package, &mut deps);

            // Add the package dependencies to the tree as nodes, then assign the edges.
            for (_, dep) in package_deps.iter() {
                let child_idx = add_node(dep, &mut deps);

                println!("{}", dep.identifier);

                deps.add_edge(parent_idx, child_idx, ());
                queue.push_back(dep.identifier.clone());
            }

            dep_map.extend(package_deps);
            dep_map.insert(package.identifier.to_loose_ident_string(), package);

            println!("end");
        }

        println!("{:?}", Dot::with_config(&deps, &[Config::EdgeNoLabel]));

        let topo_thing = petgraph::algo::toposort(&deps, None).unwrap();
        for thing in topo_thing {
            println!("[{}] {}", thing.index(), deps[thing]);
        }

        panic!("");

        let packages_to_install = dep_map.into_values().collect::<Vec<_>>();
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

        let jobs = self
            .packages_to_install
            .iter()
            .map(|package| package.add(&self.project, multi.add_bar()));

        try_join_all(jobs).await?;

        self.lockfile.merge(&self.packages_to_install);
        self.lockfile.commit()?;

        Ok(())
    }
}
