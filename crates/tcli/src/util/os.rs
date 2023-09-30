use serde::{Serialize, Deserialize};
use std::fmt::{Display, Formatter};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub enum OS {
    Windows,
    MacOS,
    Linux,
}

impl Display for OS {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        let str_name = match self {
            OS::Windows => "Windows",
            OS::MacOS => "MacOS",
            OS::Linux => "Linux",
        };

        write!(f, "{str_name}")
    }
}

impl From<String> for OS {
    fn from(value: String) -> Self {
        let lowercase = value.to_lowercase();

        match lowercase.as_str() {
            "windows" => OS::Windows,
            "macos" => OS::MacOS,
            "linux" => OS::Linux,
            _ => panic!("'{value}' is not a valid OS name."),
        }
    }
}
