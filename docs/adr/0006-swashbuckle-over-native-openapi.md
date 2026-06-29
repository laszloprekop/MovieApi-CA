# Swashbuckle for OpenAPI generation, Swagger UI + Scalar for the UI

The project started on .NET 10's **native** `Microsoft.AspNetCore.OpenApi` (`AddOpenApi` / `MapOpenApi`).
For the documentation stretch goal we **replace it with Swashbuckle** (`Swashbuckle.AspNetCore`:
`AddSwaggerGen` + `UseSwaggerUI`) and serve **both** Swagger UI and **Scalar** over the generated docs.

**Why revert to Swashbuckle** (a future reader will reasonably ask): the course materials teach the
Swashbuckle stack explicitly — XML-comment ingestion via `IncludeXmlComments`, per-version `SwaggerDoc`s —
and the deliverable is meant to track those materials. Swashbuckle also has the most turnkey integration
with `Asp.Versioning.Mvc.ApiExplorer` for emitting one document per API version. We keep it *modern*
by generating the per-version documents from `IApiVersionDescriptionProvider` (an
`IConfigureOptions<SwaggerGenOptions>`) instead of hardcoding `"v1"`/`"v2"`, and by adding **Scalar** as a
second, contemporary reference UI alongside classic Swagger UI.

Considered and rejected: keeping native `AddOpenApi` + Scalar only (most modern, and .NET 10 can read XML
comments natively) — rejected because it diverges from the materials and gives weaker multi-version
Swagger-UI ergonomics, and the brief wants "Swagger" specifically.

**Consequence:** `AddOpenApi`/`MapOpenApi` and the `Microsoft.AspNetCore.OpenApi` package reference are
removed in favour of `Swashbuckle.AspNetCore` + `Scalar.AspNetCore`.
