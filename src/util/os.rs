use std::fmt::{Display, Formatter};

use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug, Copy, Clone)]
pub enum OS {
    Windows,
    MacOS,
    Linux,
}

#[derive(Serialize, Deserialize, Debug, Copy, Clone)]

pub enum ARCH {
    X86_64,
    X86,
    AArch64,
    Arm,
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

impl Display for ARCH {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        let str_name = match self {
            ARCH::X86_64 => "x86_64",
            ARCH::X86 => "x86",
            ARCH::AArch64 => "aarch64",
            ARCH::Arm => "arm",
        };

        write!(f, "{str_name}")
    }
}

impl From<String> for ARCH {
    fn from(value: String) -> Self {
        let lowercase = value.to_lowercase();

        match lowercase.as_str() {
            "x86_64" => ARCH::X86_64,
            "x86" => ARCH::X86,
            "aarch64" => ARCH::AArch64,
            "arm" => ARCH::Arm,
            _ => panic!("'{value}' is not a valid architecture name."),
        }
    }
}
