use indicatif::{MultiProgress, ProgressBar};

use crate::ui::PROGRESS_STYLE;

pub trait Reporter {
    fn create_progress(&self) -> Box<dyn Progress>;
}

pub struct IndicatifReporter;

impl Reporter for IndicatifReporter {
    fn create_progress(&self) -> Box<dyn Progress> {
        Box::new(MultiProgress::new())
    }
}

pub struct VoidReporter;

impl Reporter for VoidReporter {
    fn create_progress(&self) -> Box<dyn Progress> {
        Box::new(VoidProgress)
    }
}

pub trait Progress {
    fn add_bar(&self) -> Box<dyn ProgressBarTrait>;
}

impl Progress for MultiProgress {
    fn add_bar(&self) -> Box<dyn ProgressBarTrait> {
        Box::new(self.add(ProgressBar::new(10).with_style(PROGRESS_STYLE.clone())))
    }
}

pub struct VoidProgress;

impl Progress for VoidProgress {
    fn add_bar(&self) -> Box<dyn ProgressBarTrait> {
        Box::new(VoidProgressBar)
    }
}

pub trait ProgressBarTrait {
    fn inc(&self, count: u64);
    fn set_length(&self, length: u64);
    fn println(&self, message: &str);
    fn set_message(&self, message: String);
    fn finish(&self);
    fn finish_and_clear(&self);
    fn finish_with_message(&self, message: String);
}

impl ProgressBarTrait for ProgressBar {
    fn inc(&self, count: u64) {
        self.inc(count);
    }

    fn set_length(&self, length: u64) {
        self.set_length(length);
    }

    fn println(&self, message: &str) {
        self.println(message);
    }

    fn set_message(&self, message: String) {
        self.set_message(message)
    }

    fn finish(&self) {
        self.finish();
    }

    fn finish_and_clear(&self) {
        self.finish_and_clear();
    }

    fn finish_with_message(&self, message: String) {
        self.finish_with_message(message);
    }
}

struct VoidProgressBar;

#[allow(unused)]
impl ProgressBarTrait for VoidProgressBar {
    fn inc(&self, count: u64) {}

    fn set_length(&self, length: u64) {}

    fn println(&self, message: &str) {}

    fn set_message(&self, message: String) {}

    fn finish(&self) {}

    fn finish_and_clear(&self) {}

    fn finish_with_message(&self, message: String) {}
}
