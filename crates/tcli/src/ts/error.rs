#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("A generic API error occured.")]
    GenericApiError(reqwest::Error),

    #[error("Failed to serialize response as JSON.")]
    SerializationFailure(reqwest::Error),
}
