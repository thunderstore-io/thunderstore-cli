use std::collections::HashMap;
use std::fs;
use std::fs::File;
use std::io::{ErrorKind, Write};
use std::path::{Path, PathBuf};

use futures::future::try_join_all;
pub use publish::publish;
use zip::write::FileOptions;

use crate::error::{Error, IoResultToTcli};
use crate::package::{resolver, Package};
use crate::project::manifest::ProjectManifest;
use crate::project::overrides::ProjectOverrides;
use crate::ts::package_manifest::PackageManifestV1;
use crate::ts::package_reference::PackageReference;
use crate::ui::reporter::Reporter;

use self::lock::LockFile;

pub mod lock;
pub mod manifest;
pub mod overrides;
mod publish;

pub enum ProjectKind {
    Dev(ProjectOverrides),
    Profile,
}

pub struct Project {
    pub base_dir: PathBuf,
    pub state_dir: PathBuf,
    pub manifest_path: PathBuf,
    pub lockfile_path: PathBuf,
    pub game_registry_path: PathBuf,
}

impl Project {
    pub fn open(project_dir: &Path) -> Result<Self, Error> {
        // TODO: Validate that the following paths exist.

        Ok(Project {
            base_dir: project_dir.to_path_buf(),
            state_dir: project_dir.join(".tcli/project_state"),
            manifest_path: project_dir.join("Thunderstore.toml"),
            lockfile_path: project_dir.join("Thunderstore.lock"),
            game_registry_path: project_dir.join(".tcli/game_registry.json"),
        })
    }

    /// Create a new project within the given directory.
    pub fn create_new(
        project_dir: &Path,
        overwrite: bool,
        project_kind: ProjectKind,
    ) -> Result<Project, Error> {
        if project_dir.is_file() {
            return Err(Error::ProjectDirIsFile(project_dir.into()));
        }

        if !project_dir.is_dir() {
            fs::create_dir(project_dir).map_fs_error(project_dir)?;
        }

        let manifest = match &project_kind {
            ProjectKind::Dev(overrides) => {
                let mut manifest = ProjectManifest::default_dev_project();
                manifest.apply_overrides(overrides.clone())?;
                manifest
            }
            ProjectKind::Profile => ProjectManifest::default_profile_project(),
        };

        let mut options = File::options();
        options.write(true);
        if overwrite {
            options.create(true);
        } else {
            options.create_new(true);
        }

        let manifest_path = project_dir.join("Thunderstore.toml");
        let mut manifest_file = match options.open(&manifest_path) {
            Ok(x) => Ok(x),
            Err(e) if e.kind() == ErrorKind::AlreadyExists => {
                Err(Error::ProjectAlreadyExists(manifest_path.clone()))
            }
            Err(e) => Err(Error::FileIoError(manifest_path.to_path_buf(), e)),
        }?;

        write!(
            manifest_file,
            "{}",
            toml::to_string_pretty(&manifest).unwrap()
        )?;

        let project_state = project_dir.join(".tcli/project_state");
        fs::create_dir_all(&project_state)?;

        let project = Project {
            base_dir: project_dir.to_path_buf(),
            state_dir: project_state,
            manifest_path,
            lockfile_path: project_dir.join("Thunderstore.lock"),
            game_registry_path: project_dir.join(".tcli/game_registry.json"),
        };

        // Stop here if all we need is a profile.
        if matches!(project_kind, ProjectKind::Profile) {
            return Ok(project);
        }

        let package = manifest.package.as_ref().unwrap();

        let icon_path = project_dir.join("icon.png");
        match File::options()
            .write(true)
            .create_new(true)
            .open(&icon_path)
        {
            Ok(mut f) => f
                .write_all(include_bytes!("../../resources/icon.png"))
                .unwrap(),
            Err(e) if e.kind() == ErrorKind::AlreadyExists => {}
            Err(e) => Err(Error::FileIoError(icon_path, e))?,
        }

        let readme_path = project_dir.join("README.md");
        match File::options()
            .write(true)
            .create_new(true)
            .open(&readme_path)
        {
            Ok(mut f) => write!(
                f,
                include_str!("../../resources/readme_template.md"),
                package.namespace, package.name, package.description
            )?,
            Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => {}
            Err(e) => return Err(Error::FileIoError(readme_path, e)),
        }

        let dist_dir = project.base_dir.join("dist");
        if !dist_dir.exists() {
            fs::create_dir(dist_dir)?;
        }

        Ok(project)
    }

    /// Add one or more packages to this project. 
    /// 
    /// Note: This function does not COMMIT the packages, it only adds them to the project manifest.
    pub fn add_packages(&self, packages: &[PackageReference]) -> Result<(), Error> {
        let mut manifest = ProjectManifest::read_from_file(&self.manifest_path)?;
        let mut manifest_deps = manifest.dependencies.dependencies.clone();

        // Merge the manifest's dependencies with the given packages.
        // The rule here is:
        // 1. Add if the package does not exist within the manifest.
        // 2. Replace with given version if manifest.version < given.version.
        let manifest_index = manifest_deps
            .iter()
            .enumerate()
            .map(|(index, x)| (x.to_loose_ident_string(), index))
            .collect::<HashMap<_, _>>();

        for package in packages.iter() {
            match manifest_index.get(&package.to_loose_ident_string()) {
                Some(x) if manifest_deps[*x].version < package.version => {
                    manifest_deps[*x] = package.clone();
                }

                None => {
                    manifest_deps.push(package.clone());
                }

                _ => (),
            }
        }

        manifest.dependencies.dependencies = manifest_deps;
        manifest.write_to_file(&self.manifest_path)?;

        Ok(())
    }

    /// Commit changes made to the project manifest to the project.
    pub async fn commit(&self, reporter: Box<dyn Reporter>) -> Result<(), Error> {
        let manifest = ProjectManifest::read_from_file(&self.manifest_path)?;

        let package_graph = resolver::resolve_packages(manifest.dependencies.dependencies).await?;
        let packages = package_graph.digest();
        
        let resolved_packages = try_join_all(packages
            .iter()
            .rev()
            .map(|x| async move {
                Package::resolve_new(*x).await
            })).await?;

        // Download / install each package as needed.
        let multi = reporter.create_progress();
        let jobs = resolved_packages
            .iter()
            .map(|package| async {
                package.add(&self.state_dir, multi.add_bar()).await
            });

        try_join_all(jobs).await?;

        // For now we can regenerate the lockfile from scratch.
        let mut lockfile = LockFile::open_or_new(&self.lockfile_path)?;
        lockfile.merge(&resolved_packages);
        lockfile.commit()?;

        Ok(())
    }

    pub fn build(&self, overrides: ProjectOverrides) -> Result<PathBuf, Error> {
        let mut manifest = self.get_manifest()?;
        manifest.apply_overrides(overrides)?;
                
        let project_dir = manifest
            .project_dir
            .as_deref()
            .expect("Project should be loaded from a file to build");

        let package = manifest
            .package
            .as_ref()
            .ok_or(Error::MissingTable("package"))?;
        
        let build = manifest
            .build
            .as_ref()
            .ok_or(Error::MissingTable("build"))?;

        let output_dir = project_dir.join(&build.outdir);
        match fs::create_dir_all(&output_dir) {
            Ok(_) => Ok(()),
            Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => Ok(()),
            Err(e) => Err(Error::FileIoError(output_dir.clone(), e)),
        }?;

        let output_path = output_dir.join(format!(
            "{}-{}-{}.zip",
            package.namespace, package.name, package.version
        ));

        let mut zip = zip::ZipWriter::new(
            File::options()
                .create(true)
                .write(true)
                .open(&output_path)
                .map_fs_error(&output_path)?,
        );

        for copy in &build.copy {
            let source_path = project_dir.join(&copy.source);

            // first elem is always the root, even when the path given is to a file
            for file in walkdir::WalkDir::new(&source_path).follow_links(true) {
                let file = file?;

                let inner_path = file
                    .path()
                    .strip_prefix(&source_path)
                    .expect("Path was made by walking source, but was not rooted in source?");

                if file.file_type().is_dir() {
                    zip.add_directory(
                        copy.target.join(inner_path).to_string_lossy(),
                        FileOptions::default(),
                    )?;
                } else if file.file_type().is_file() {
                    zip.start_file(
                        copy.target.join(inner_path).to_string_lossy(),
                        FileOptions::default(),
                    )?;
                    std::io::copy(
                        &mut File::open(file.path()).map_fs_error(file.path())?,
                        &mut zip,
                    )?;
                } else {
                    unreachable!("paths should always be either a file or a dir")
                }
            }
        }

        zip.start_file("manifest.json", FileOptions::default())?;
        write!(
            zip,
            "{}",
            serde_json::to_string_pretty(&PackageManifestV1::from_manifest(
                package.clone(),
                manifest.dependencies.dependencies.clone()
            ))
            .unwrap()
        )?;

        let icon_path = project_dir.join(&build.icon);
        zip.start_file("icon.png", FileOptions::default())?;
        std::io::copy(
            &mut File::open(&icon_path).map_fs_error(icon_path)?,
            &mut zip,
        )?;

        let readme_path = project_dir.join(&build.readme);
        zip.start_file("README.md", FileOptions::default())?;
        write!(
            zip,
            "{}",
            fs::read_to_string(&readme_path).map_fs_error(readme_path)?
        )?;

        zip.finish()?;

        Ok(output_path)
    }

    pub fn get_manifest(&self) -> Result<ProjectManifest, Error> {
        ProjectManifest::read_from_file(&self.manifest_path)
    }

    pub fn get_lockfile(&self) -> Result<LockFile, Error> {
        LockFile::open_or_new(&self.lockfile_path)
    }
}

