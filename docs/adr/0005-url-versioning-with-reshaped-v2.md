# URL-segment API versioning, with v2 reshaping Genre

We version the API with **`Asp.Versioning`** using the **URL-segment** scheme
(`/api/v{version:apiVersion}/...`) — the lecture's "vanligast" strategy and the clearest one in
routing and Swagger. `v1` is the default (`AssumeDefaultVersionWhenUnspecified`); every existing
controller (Movies, Actors, Reviews) moves under `/api/v1/...`, and **Movies** additionally exposes a
`v2`.

The `v2` change is a **deliberate breaking change**, not an additive one: `MovieDto.Genre` is a single
joined string left over from before genres became many-to-many (ADR 0002). `v1` keeps that lossy string
for backward compatibility; `v2` replaces it with `Genres` as a string array that honestly reflects the
model. This is exactly the lecture's `firstName → givenName` scenario — the point of versioning is to
ship a contract break without breaking existing clients.

Considered and rejected: query-string / header / media-type versioning (less visible, harder to test,
weaker Swagger grouping); and a purely additive v2 (simpler, but doesn't demonstrate the hard part).

**Consequence:** URL-segment versioning is a breaking change to *all* existing routes — `/api/movies`
stops resolving; clients must call `/api/v1/movies`. That is accepted and is the reason v1 is the
assumed default.
