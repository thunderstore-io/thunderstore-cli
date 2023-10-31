use std::fs;
use std::fs::File;
use std::io::{ErrorKind, Write};
use std::path::{Path, PathBuf};

pub use publish::publish;
use zip::write::FileOptions;

use crate::error::{Error, IoResultToTcli};
use crate::project::manifest::ProjectManifest;
use crate::project::overrides::ProjectOverrides;
use crate::ts::package_manifest::PackageManifestV1;

pub mod lock;
pub mod manifest;
pub mod overrides;
mod publish;

pub enum ProjectKind {
    Dev(ProjectOverrides),
    Profile,
}

#[derive(Clone)]
pub struct ProjectPath(PathBuf);

impl ProjectPath {
    pub fn new(path: &Path) -> Result<ProjectPath, Error> {
        let path = path.to_path_buf();

        if !path.exists() {
            return Err(Error::NoProjectFile(path));
        }

        let root_dir = if path.is_file() {
            path.parent().unwrap().to_path_buf()
        } else {
            path
        };

        if !root_dir.join("Thunderstore.toml").is_file() {
            return Err(Error::NoProjectFile(root_dir));
        }

        if !root_dir.join(".tcli/").is_dir() {
            fs::create_dir(root_dir.join(".tcli/"))?;
        }

        Ok(ProjectPath(root_dir))
    }

    pub fn path(&self) -> &Path {
        self.0.as_path()
    }
}

struct Project {
    base_dir: PathBuf,
    state_dir: PathBuf,
    manifest_path: PathBuf,
}

impl Project {
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
        };

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

        Ok(project)
    }
}

pub fn create_new(
    project_path: &Path,
    overwrite: bool,
    project_kind: ProjectKind,
) -> Result<(), Error> {
    let project_dir = project_path.parent().unwrap_or("./".as_ref());

    if project_dir.is_file() {
        return Err(Error::ProjectDirIsFile(project_dir.into()));
    }

    if !project_dir.is_dir() {
        fs::create_dir(project_dir).map_fs_error(project_dir)?;
    }

    let manifest = match project_kind {
        ProjectKind::Dev(overrides) => {
            let mut manifest = ProjectManifest::default_dev_project();
            manifest.apply_overrides(overrides)?;
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

    let mut manifest_file = options
        .open(project_path)
        .map_err(move |e| match e.kind() {
            std::io::ErrorKind::AlreadyExists => {
                Error::ProjectAlreadyExists(project_path.to_path_buf())
            }
            _ => Error::FileIoError(project_path.to_path_buf(), e),
        })?;

    write!(
        manifest_file,
        "{}",
        toml::to_string_pretty(&manifest).unwrap()
    )
    .unwrap();

    if manifest.package.is_none() {
        return Ok(());
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
        Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => {}
        Err(e) => Err(Error::FileIoError(icon_path, e))?,
    }

    let readme_path = project_dir.join("README.md");
    match File::options()
        .write(true)
        .create_new(true)
        .open(&readme_path)
    {
        Ok(mut f) => {
            write!(
                f,
                include_str!("../../resources/readme_template.md"),
                package.namespace, package.name, package.description
            )
            .unwrap();
        }
        Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => {}
        Err(e) => return Err(Error::FileIoError(readme_path, e)),
    }

    Ok(())
}

pub fn build(manifest: &ProjectManifest) -> Result<PathBuf, Error> {
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

    let output_path = output_dir.join(format!(
        "{}-{}-{}.zip",
        package.namespace, package.name, package.version
    ));

    match fs::create_dir_all(output_path.parent().unwrap()) {
        Ok(_) => Ok(()),
        Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => Ok(()),
        Err(e) => Err(Error::FileIoError(output_path.clone(), e)),
    }?;

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
