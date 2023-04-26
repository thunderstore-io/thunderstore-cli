use std::collections::HashMap;
use std::fmt::Formatter;

use serde::de::{MapAccess, SeqAccess, Visitor};
use serde::{Deserialize, Deserializer, Serialize, Serializer};

#[derive(Serialize, Deserialize, Default)]
pub struct ProjectManifest {
    config: ConfigData,
    package: PackageData,
    build: BuildData,
    publish: PublishData,
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct ConfigData {
    schema_version: String,
}

impl Default for ConfigData {
    fn default() -> Self {
        ConfigData {
            schema_version: "0.0.1".into(),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct PackageData {
    namespace: String,
    name: String,
    version_number: String,
    description: String,
    website_url: String,
    contains_nsfw_content: bool,
    dependencies: HashMap<String, String>,
}

impl Default for PackageData {
    fn default() -> Self {
        PackageData {
            namespace: "AuthorName".into(),
            name: "PackageName".into(),
            version_number: "0.0.1".into(),
            description: "Example mod description".into(),
            website_url: "https://thunderstore.io".into(),
            contains_nsfw_content: false,
            dependencies: HashMap::from([("AuthorName-PackageName".into(), "0.0.1".into())]),
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
#[serde(rename_all = "camelCase")]
struct BuildData {
    icon: String,
    readme: String,
    outdir: String,
    copy: Vec<CopyPath>,
}

impl Default for BuildData {
    fn default() -> Self {
        BuildData {
            icon: "./icon.png".into(),
            readme: "./README.md".into(),
            outdir: "./build".into(),
            copy: vec![CopyPath::default()],
        }
    }
}

#[derive(Serialize, Deserialize, Debug)]
struct CopyPath {
    source: String,
    target: String,
}

impl Default for CopyPath {
    fn default() -> Self {
        CopyPath {
            source: "./dist".into(),
            target: "".into(),
        }
    }
}

#[derive(Debug)]
enum Categories {
    Old(Vec<String>),
    New(HashMap<String, Vec<String>>),
}

impl Serialize for Categories {
    fn serialize<S: Serializer>(&self, serializer: S) -> Result<S::Ok, S::Error> {
        match self {
            Categories::Old(v) => v.serialize(serializer),
            Categories::New(map) => map.serialize(serializer),
        }
    }
}

impl<'de> Deserialize<'de> for Categories {
    fn deserialize<D: Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        deserializer.deserialize_any(CategoriesVisitor)
    }
}

struct CategoriesVisitor;

impl<'v> Visitor<'v> for CategoriesVisitor {
    type Value = Categories;

    fn expecting(&self, formatter: &mut Formatter) -> std::fmt::Result {
        write!(
            formatter,
            "either an array of strings or a map of strings to arrays of strings."
        )
    }

    fn visit_seq<A: SeqAccess<'v>>(self, mut seq: A) -> Result<Self::Value, A::Error> {
        let mut res = if let Some(hint) = seq.size_hint() {
            Vec::with_capacity(hint)
        } else {
            vec![]
        };

        while let Some(next) = seq.next_element()? {
            res.push(next);
        }

        Ok(Categories::Old(res))
    }

    fn visit_map<A: MapAccess<'v>>(self, mut map: A) -> Result<Self::Value, A::Error> {
        let mut res = if let Some(hint) = map.size_hint() {
            HashMap::with_capacity(hint)
        } else {
            HashMap::new()
        };

        while let Some((key, val)) = map.next_entry()? {
            res.insert(key, val);
        }

        Ok(Categories::New(res))
    }
}

#[derive(Serialize, Deserialize, Debug)]
struct PublishData {
    repository: String,
    communities: Vec<String>,
    categories: Categories,
}

impl Default for PublishData {
    fn default() -> Self {
        PublishData {
            repository: "https://thunderstore.io".into(),
            communities: vec!["riskofrain2".to_string()],
            categories: Categories::New(HashMap::from([(
                "riskofrain2".into(),
                vec!["items".into(), "skills".into()],
            )])),
        }
    }
}
