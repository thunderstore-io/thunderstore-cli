use std::borrow::Cow;

use indicatif::{MultiProgress, ProgressBar, ProgressStyle};

/// ProgressReporter is a wrapper around the ProgressBar struct.
///
/// This trait allows for recieving functions to report progress without needing to handle the
/// details about *where* said progress is written to.
pub trait ProgressReporter {
    fn new(size: usize) -> Self;
    fn from_multi(multi: &MultiProgress, size: usize) -> Self;
    fn set_style(&self, style: ProgressStyle);
    fn set_length(&self, len: usize);
    fn set_message(&self, msg: impl Into<Cow<'static, str>>);
    fn println(&self, msg: impl AsRef<str>);
    fn finish(&self);
    fn finish_and_clear(&self);
    fn finish_with_message(&self, msg: impl Into<Cow<'static, str>>);
    fn inc(&self, delta: usize);
}

pub struct GenericProgressReporter {
    progress: ProgressBar,
}

impl ProgressReporter for GenericProgressReporter {
    fn new(len: usize) -> Self {
        GenericProgressReporter {
            progress: ProgressBar::new(len as _),
        }
    }

    fn from_multi(multi: &MultiProgress, len: usize) -> Self {
        GenericProgressReporter {
            progress: multi.add(ProgressBar::new(len as _)),
        }
    }

    fn set_style(&self, style: ProgressStyle) {
        self.progress.set_style(style);
    }

    fn set_length(&self, len: usize) {
        self.progress.set_length(len as _);
    }

    fn set_message(&self, msg: impl Into<Cow<'static, str>>) {
        self.progress.set_message(msg);
    }

    fn println(&self, msg: impl AsRef<str>) {
        self.progress.println(msg);
    }

    fn finish(&self) {
        self.progress.finish()
    }

    fn finish_and_clear(&self) {
        self.progress.finish_and_clear();
    }

    fn finish_with_message(&self, msg: impl Into<Cow<'static, str>>) {
        self.progress.finish_with_message(msg);
    }

    fn inc(&self, delta: usize) {
        self.progress.inc(delta as _);
    }
}

pub struct VoidProgressReporter;

#[allow(unused_variables)]
impl ProgressReporter for VoidProgressReporter {
    fn new(size: usize) -> Self {
        VoidProgressReporter {}
    }

    fn from_multi(multi: &MultiProgress, size: usize) -> Self {
        VoidProgressReporter {}
    }

    fn set_style(&self, style: ProgressStyle) {}

    fn set_length(&self, len: usize) {}

    fn set_message(&self, msg: impl Into<Cow<'static, str>>) {}

    fn println(&self, msg: impl AsRef<str>) {}

    fn finish(&self) {}

    fn finish_and_clear(&self) {}

    fn finish_with_message(&self, msg: impl Into<Cow<'static, str>>) {}

    fn inc(&self, delta: usize) {}
}
