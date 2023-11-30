use crate::error::Error;
use crate::ts::v1::models::ecosystem::EcosystemSchema;
use crate::ts::CLIENT;

pub async fn get_schema() -> Result<EcosystemSchema, Error> {
    Ok(CLIENT
        .get("https://gcdn.thunderstore.io/static/dev/schema/ecosystem-schema.0.0.11.json")
        .send()
        .await?
        .error_for_status()?
        .json()
        .await?)
}
