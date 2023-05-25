use indicatif::ProgressStyle;  

pub static PROGRESS_STYLE: ProgressStyle = ProgressStyle::with_template("[{elapsed}] {bytes}/{total_bytes} {wide_bar}").unwrap();
