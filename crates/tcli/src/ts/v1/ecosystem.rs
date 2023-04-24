use crate::ts::CLIENT;
use crate::error::Error;
use crate::ts::v1::models::ecosystem::EcosystemSchema;

pub async fn get_schema() -> Result<EcosystemSchema, Error> {
    let response = CLIENT.get("https://gcdn.thunderstore.io/static/dev/schema/ecosystem-schema.0.0.2.json")
        .send()
        .await
        .map_err(Error::GenericApiError)?;

    response.json().await.map_err(Error::SerializationFailure)
}
