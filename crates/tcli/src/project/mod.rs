use std::fs;
use std::fs::File;
use std::io::Write;
use std::path::Path;

use zip::write::FileOptions;

use crate::error::{Error, IoResultToTcli};
use crate::project::manifest::ProjectManifest;
use crate::ts::package_manifest::PackageManifestV1;
use crate::ts::version::Version;

pub mod manifest;

pub fn create_new(
    config_path: impl AsRef<Path>,
    overwrite: bool,
    namespace: Option<String>,
    name: Option<String>,
    version: Option<Version>,
) -> Result<(), Error> {
    let config_path = config_path.as_ref();

    let project_dir = config_path.parent().unwrap_or("./".as_ref());
    let config_filename = config_path
        .file_name()
        .ok_or_else(|| Error::PathIsDirectory(config_path.into()))?;

    if project_dir.is_file() {
        return Err(Error::ProjectDirIsFile(project_dir.into()));
    }

    if !project_dir.is_dir() {
        fs::create_dir(project_dir).map_fs_error(project_dir)?;
    }

    let manifest_path = project_dir.join(config_filename);

    let manifest = {
        let mut manifest = ProjectManifest::default_dev_project();
        let package = manifest.package.as_mut().unwrap();
        if let Some(namespace) = namespace {
            package.namespace = namespace;
        }
        if let Some(name) = name {
            package.name = name;
        }
        if let Some(version) = version {
            package.version = version;
        }
        manifest
    };
    let package = manifest.package.as_ref().unwrap();

    let mut options = File::options();
    options.write(true);
    if overwrite {
        options.create(true);
    } else {
        options.create_new(true);
    }

    let mut manifest_file = options
        .open(&manifest_path)
        .map_err(move |e| match e.kind() {
            std::io::ErrorKind::AlreadyExists => Error::ProjectAlreadyExists(manifest_path),
            _ => Error::FileIoError(manifest_path, e),
        })?;

    write!(
        manifest_file,
        "{}",
        toml::to_string_pretty(&manifest).unwrap()
    )
    .unwrap();

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

pub fn build(
    config_path: impl AsRef<Path>,
    output_path: Option<impl AsRef<Path>>,
    namespace: Option<String>,
    name: Option<String>,
    version: Option<Version>,
) -> Result<(), Error> {
    let config_path = config_path.as_ref();

    if !config_path.is_file() {
        return Err(Error::NoProjectFile(config_path.into()));
    }

    let project_dir = config_path.parent().unwrap_or(Path::new("./"));

    let manifest = {
        let mut manifest: ProjectManifest =
            toml::from_str(&fs::read_to_string(config_path).map_fs_error(config_path)?)?;

        let package = manifest
            .package
            .as_mut()
            .ok_or(Error::MissingManifestField("package".into()))?;
        if let Some(namespace) = namespace {
            package.namespace = namespace;
        }
        if let Some(name) = name {
            package.name = name;
        }
        if let Some(version) = version {
            package.version = version;
        }

        manifest
    };
    let package = manifest.package.as_ref().unwrap();
    let build = manifest
        .build
        .as_ref()
        .ok_or(Error::MissingManifestField("build".into()))?;

    let output_path = output_path
        .as_ref()
        .map(|p| p.as_ref().to_path_buf())
        .unwrap_or(project_dir.join(&build.outdir))
        .join(format!(
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
            .map_fs_error(output_path)?,
    );

    for copy in &build.copy {
        let source_path = project_dir.join(&copy.source);

        // first elem is always the root, even when the path given is to a file
        for file in walkdir::WalkDir::new(&source_path) {
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

    Ok(())
}
