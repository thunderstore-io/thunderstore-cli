use crate::error::Error;
use crate::ts::v1::models::ecosystem::EcosystemSchema;
use crate::ts::CLIENT;

pub async fn get_schema() -> Result<EcosystemSchema, Error> {
    let response = CLIENT
        .get("https://gcdn.thunderstore.io/static/dev/schema/ecosystem-schema.0.0.8.json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}
