use indicatif::ProgressStyle;
use once_cell::sync::Lazy;

pub static PROGRESS_STYLE: Lazy<ProgressStyle> = Lazy::new(|| {
    ProgressStyle::with_template("[{elapsed}] {bytes}/{total_bytes} {wide_bar}").unwrap()
});
