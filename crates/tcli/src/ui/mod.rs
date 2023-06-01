pub mod reporter;

use indicatif::ProgressStyle;
use once_cell::sync::Lazy;

pub static PROGRESS_STYLE: Lazy<ProgressStyle> = Lazy::new(|| {
    ProgressStyle::with_template("[{spinner}] {msg:>20} {bytes}/{total_bytes}")
        .unwrap()
        .progress_chars("█░-")
});
