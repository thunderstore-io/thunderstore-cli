use std::path::PathBuf;

use crate::ts::version::Version;

#[derive(Clone, Debug, Default)]
pub struct ProjectOverrides {
    pub(in crate::project) namespace: Option<String>,
    pub(in crate::project) name: Option<String>,
    pub(in crate::project) version: Option<Version>,
    pub(in crate::project) output_dir: Option<PathBuf>,
    pub(in crate::project) repository: Option<String>,
}

impl ProjectOverrides {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn namespace_override(self, namespace: Option<String>) -> Self {
        Self {
            namespace: namespace.or(self.namespace),
            ..self
        }
    }

    pub fn name_override(self, name: Option<String>) -> Self {
        Self {
            name: name.or(self.name),
            ..self
        }
    }

    pub fn version_override(self, version: Option<Version>) -> Self {
        Self {
            version: version.or(self.version),
            ..self
        }
    }

    pub fn output_dir_override(self, output_dir: Option<PathBuf>) -> Self {
        Self {
            output_dir: output_dir.or(self.output_dir),
            ..self
        }
    }

    pub fn repository_override(self, repository: Option<String>) -> Self {
        Self {
            repository: repository.or(self.repository),
            ..self
        }
    }
}
