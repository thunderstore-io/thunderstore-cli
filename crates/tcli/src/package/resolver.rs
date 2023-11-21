use std::collections::{HashMap, VecDeque};

use petgraph::prelude::NodeIndex;
use petgraph::visit::Dfs;
use petgraph::{algo, Directed, Graph};

use crate::error::Error;
use crate::package::index::{self, PackageIndex};
use crate::ts::package_reference::PackageReference;

pub enum Granularity {
    All,
    IgnoreVersion,
    LesserVersion,
    GreaterVersion,
}

pub struct DependencyGraph {
    graph: Graph::<PackageReference, (), Directed>,
    index: HashMap<String, NodeIndex>,
}

impl DependencyGraph {
    pub fn new() -> Self {
        DependencyGraph {
            graph: Graph::new(),
            index: HashMap::new(),
        }
    }

    /// Add a node to the dependency graph, replacing if it already exists within the graph
    /// but is of a lesser semver.
    pub fn add(&mut self, value: PackageReference) {
        let node_index = *self.index
            .entry(value.to_loose_ident_string())
            .or_insert_with(|| self.graph.add_node(value.clone()));

        let graph_value = &self.graph[node_index];

        if graph_value.version < value.version {
            self.graph[node_index] = value;
        }
    }

    /// Add an edge between two values in the graph.
    pub fn add_edge(&mut self, parent: &PackageReference, child: &PackageReference) {
        let parent_index = self.index[&parent.to_loose_ident_string()];
        let child_index = self.index[&child.to_loose_ident_string()];

        self.graph.add_edge(parent_index, child_index, ());
    }

    /// Determine if the given value exists within the graph within the specified granularity.
    pub fn exists(&self, value: &PackageReference, granularity: Granularity) -> bool {
        let loose = value.to_loose_ident_string();
        let node_index = self.index.get(&loose);

        if let None = node_index {
            return false;
        }

        let node_index = node_index.unwrap();
        let graph_value = &self.graph[*node_index];

        match granularity {
            Granularity::All => graph_value.version == value.version,
            Granularity::IgnoreVersion => true,
            Granularity::LesserVersion => graph_value.version < value.version,
            Granularity::GreaterVersion => graph_value.version > value.version,
        }
    }

    /// Get the dependencies of value's node within the graph.
    ///
    /// The resultant Vec is not guarenteed to be in "install order"; instead it is ordered
    /// by traversal cost.
    pub fn get_dependencies(&self, value: &PackageReference) -> Option<Vec<&PackageReference>> {
        let loose = value.to_loose_ident_string();
        let node_index = self.index.get(&loose)?;

        // Compute the shortest path to every child node.
        let mut children  = algo::dijkstra(&self.graph, *node_index, None, |_| 1)
            .into_iter()
            .map(|(index, cost)| (&self.graph[index], cost))
            .collect::<Vec<_>>();

        // Sort the children by their cost, which describes the number of "steps" that were required
        // to path to each node.
        children.sort_by(|first, second| first.1.cmp(&second.1));

        Some(children.into_iter().map(|(package_ref, _)| package_ref).collect::<Vec<_>>())
    }

    /// Digest the dependency graph, resolving its contents into a DFS-ordered list of package references.
    pub fn digest(&self) -> Vec<&PackageReference> {
        let mut dfs= Dfs::new(&self.graph, NodeIndex::new(0));
        let mut dependencies = Vec::new();

        while let Some(element) = dfs.next(&self.graph) {
            dependencies.push(&self.graph[element]);
        }

        dependencies
    }
}

// type DependencyGraph<'a> = Graph::<&'a PackageReference, (), Directed>;

/// Generate a deduplicated list of package dependencies. This describes every package that
/// needs to be downloaded and installed for all of the root packages to function.
///
/// This takes into account:
/// 1. Packages already installed into the project.
/// 2. Dependencies specified within local packages within the cache.
/// 3. Dependencies specified within the remote repository.
pub async fn resolve_packages(
    packages: Vec<PackageReference>,
) -> Result<DependencyGraph, Error> {
    index::sync_index().await?;
    let package_index = PackageIndex::open().await?;

    let start = std::time::Instant::now();

    let mut graph = DependencyGraph::new();

    let mut iter_queue: VecDeque<&PackageReference> =
        VecDeque::from(packages.iter().collect::<Vec<_>>());

    while let Some(package_ident) = iter_queue.pop_front() {
        let package = package_index.get_package(package_ident).unwrap();

        // Add the package to the dependency graph.
        graph.add(package_ident.clone());

        for dependency in package.dependencies.iter() {
            // Queue up this dependency for processing if:
            // 1. This dependency already exists within the graph, but is a lesser version.
            // 2. This dependency does not exist within the graph.
            if !graph.exists(dependency, Granularity::GreaterVersion) {
                iter_queue.push_back(dependency);
                graph.add(dependency.clone());
            }           

            graph.add_edge(package_ident, dependency);
        }
    }

    let packages = graph.digest();

    let stop = std::time::Instant::now();
    let pkg_count = packages.len();

    println!("{} packages in {}ms", pkg_count, (stop - start).as_millis());

    Ok(graph)
}

#[cfg(test)]
mod tests {
    use std::collections::HashSet;
    use std::str::FromStr;
    use std::sync::Once;

    use crate::ts;
    use crate::package::resolver;
    use crate::ts::package_reference::PackageReference;

    static INIT: Once = Once::new();

    fn init() {
        INIT.call_once(|| {
            ts::init_repository("https://thunderstore.io", None);
        })
    }

    #[tokio::test]
    /// Test the resolver's general ability to resolve package dependencies.
    async fn test_resolver() {
        init();

        let expected = {
            let expected = vec![
                "bbepis-BepInExPack-5.4.2113",
                "RiskofThunder-BepInEx_GUI-3.0.1",
                "RiskofThunder-FixPluginTypesSerialization-1.0.3",
                "RiskofThunder-RoR2BepInExPack-1.9.0",
            ];

            expected
                .into_iter()
                .map(|x| PackageReference::from_str(x).unwrap())
                .collect::<HashSet<_>>()
        };

        let target = PackageReference::from_str("bbepis-BepInExPack-5.4.2113").unwrap();
        let got = resolver::resolve_packages(vec![target]).await.unwrap();

        for package in got.digest().iter() {
            assert!(expected.contains(package));
        }

    }

    #[tokio::test]
    /// Test the resolver's ability to handle version collisions.
    async fn test_resolver_version_hiearchy() {
        init();
        
        let expected = {
            let expected = vec![
                "bbepis-BepInExPack-5.4.2113",
                "RiskofThunder-BepInEx_GUI-3.0.1",
                "RiskofThunder-FixPluginTypesSerialization-1.0.3",
                "RiskofThunder-RoR2BepInExPack-1.9.0",
            ];

            expected
                .into_iter()
                .map(|x| PackageReference::from_str(x).unwrap())
                .collect::<HashSet<_>>()
        };

        let target = PackageReference::from_str("bbepis-BepInExPack-5.4.2113").unwrap();
        let disrupt = PackageReference::from_str("bbepis-BepInExPack-5.4.2112").unwrap();
        let got = resolver::resolve_packages(vec![target, disrupt]).await.unwrap().digest();

        for package in got.iter() {
            assert!(expected.contains(package));
        }

        assert_eq!(expected.len(), got.len());
    }
}
