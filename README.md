# MovieAPI

A Web API for a film catalogue, built as a Clean Architecture solution on .NET 10. It serves movies, their details, genres, reviews, and actors, and enforces a small set of business rules in one place (the service layer).

It was developed for an **optional assignment** in the LTU (Luleå University of Technology) / Lexicon .NET course — the "MovieAPI" continuation exercise (*Övning 6*), which refactors an earlier single-project Web API into a layered Clean Architecture solution. Being optional, it was used as a deliberate opportunity to practise the full workflow end to end rather than just meet the minimum requirements.

This repository also documents its own **LLM-supported development process**: the build is recorded as a chronological timeline of small, verified steps in [`docs/coding-steps.md`](docs/coding-steps.md), so the history of how the application took shape — the order things were done, what broke, and how it was corrected — is captured alongside the code (see [Development process](#development-process)).

---

## Features

- CRUD for movies, plus filtering by genre, year, and actor.
- Movie details (synopsis, language, budget) as a separate one-to-one entity.
- Genres modelled as a many-to-many relationship.
- Reviews and actors per movie; reviews carry a server-set `CreatedAt` timestamp.
- A background hosted service that idempotently trims the oldest reviews once a movie exceeds its cap.
- Paging with an `X-Pagination` response header.
- `PATCH` for a movie and its details via a flat JSON Patch document.
- Consistent error responses (`ProblemDetails`) through domain exceptions.
- An alternative `Result<T>` error style on the actor endpoints — in-band success/failure mapped to the same `ProblemDetails`, shown side by side with the exception-based default.
- Business rules enforced in the service layer only (see [Business rules](#business-rules)).
- Unit tests for the service layer using xUnit and NSubstitute.

---

## Tech stack

- **C# / .NET 10**, ASP.NET Core Web API
- **EF Core 10** with SQL Server (LocalDB by default)
- **AutoMapper** for entity ↔ DTO mapping
- **xUnit + NSubstitute** for tests
- **OpenAPI** for API description (Development only)

---

## Architecture

The solution is split into six projects. Dependencies point inward, and the rule is enforced by project references — only `MovieApi` (the composition root) knows about the concrete data layer.

| Project | Responsibility | References |
|---|---|---|
| **MovieCore** | Entities, DTOs, and domain contracts (repository + Unit-of-Work interfaces) | nothing |
| **MovieData** | EF Core `MovieContext`, repository + `UnitOfWork` implementations, AutoMapper profiles, migrations, seed | MovieCore |
| **MovieContracts** | Service interfaces and the `IServiceManager` facade | MovieCore |
| **MovieServices** | Service implementations, `ServiceManager`, all business rules and mapping | MovieCore, MovieContracts |
| **MoviePresentation** | Controllers; they talk only to `IServiceManager` | MovieContracts |
| **MovieApi** | Composition root: DI wiring, exception handling, request pipeline | MoviePresentation, MovieServices, MovieData |

`MovieServices.Tests` holds the service-layer unit tests.

---

## Domain model

The vocabulary below is the shared language used across the code, the seed data, and the rules. It is defined in full in [`CONTEXT.md`](CONTEXT.md).

- **Movie** — a film in the catalogue. Has core attributes (Title, Year, Duration), and is the principal of a one-to-one with MovieDetails. Has many Genres, Reviews, and Actors.
- **Title** — a movie's name. The uniqueness rule ("no two movies share a name") applies to Title.
- **MovieDetails** — the optional, heavier half of a movie: Synopsis, Language, and Budget.
- **Budget** — a movie's production cost, held on MovieDetails. Must never be negative.
- **Genre** — a category a movie belongs to (e.g. Action, Documentary). Many-to-many with Movie.
- **Documentary** — a specific genre with extra caps on actors and budget. A movie is a Documentary when "Documentary" is among its genres. The name lives once, as a domain constant.
- **Review** — a viewer's rating (1–5) and comment on one movie, with a creation time.
- **Actor** — a person who appears in movies. Many-to-many with Movie; the same actor cannot be added to the same movie twice.

---

## Business rules

All rules live in the service layer and surface as `ProblemDetails` (HTTP 400) on violation:

- A movie must reference at least one existing genre when created.
- Movie titles are unique (case-insensitive).
- Budget may not be negative.
- A Documentary may have at most **10 actors** and a budget of at most **1,000,000**.
- A movie may have at most **10 reviews**, or **5** if it is older than 20 years.
- The same actor may not be added to the same movie twice.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server **LocalDB** (installed with Visual Studio, or adjust the connection string for your own server)
- EF Core CLI tools:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- An **AutoMapper license key** (the free community key is sufficient — see [Configuration](#configuration))

### Set up and run

```bash
# 1. Restore and build
dotnet build

# 2. Provide the AutoMapper license key (stored outside the repo via user-secrets)
cd MovieApi
dotnet user-secrets set "AutoMapper:LicenseKey" "<your-community-license-key>"
cd ..

# 3. Create the database (or let step 4 apply migrations on startup)
dotnet ef database update --project MovieData --startup-project MovieApi

# 4. Run
dotnet run --project MovieApi
```

The API starts on `http://localhost:5102`. In Development, the OpenAPI document is served at `/openapi/v1.json`. Sample data is seeded automatically on first run.

---

## Configuration

| Setting | Where | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `MovieApi/appsettings.json` | Database connection. Defaults to LocalDB, database `MovieDb`. |
| `AutoMapper:LicenseKey` | user-secrets (or environment) | Required by AutoMapper. Kept out of source control. |

The default connection string is:

```
Server=(localdb)\mssqllocaldb;Database=MovieDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

---

## Database and seeding

The database is created and updated through EF Core migrations. Seed data (genres, actors, movies, reviews) is inserted automatically on startup, and the seeder is **idempotent** — it only runs when the movies table is empty.

To reset to a clean, freshly seeded database:

```bash
dotnet ef database drop -f --project MovieData --startup-project MovieApi
dotnet run --project MovieApi
```

> Schema migrations add tables; they do not back-fill data. After changing the seed, drop and re-run so the new seed takes effect.

---

## Testing

```bash
dotnet test
```

The tests cover the service layer in isolation — `IUnitOfWork` and its repositories are faked with NSubstitute, so no database is needed. They check the review and Documentary caps, the error paths (duplicate title, missing id), and a happy-path create.

---

## Trying the API

[`MovieApi/MovieApi.http`](MovieApi/MovieApi.http) contains ready-to-run requests for every endpoint, including the rule-violation cases (each with the expected status code in a comment). Open it in an HTTP client that supports `.http` files (Visual Studio, VS Code REST Client, or JetBrains Rider) and send requests against a running instance.

A few representative endpoints:

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/movies?genre=&year=&actor=&page=&pageSize=` | List/filter/page movies |
| `GET` | `/api/movies/{id}` | One movie |
| `GET` | `/api/movies/{id}/details` | Movie with details, reviews, actors |
| `POST` | `/api/movies` | Create a movie (requires `genreIds`) |
| `PUT` | `/api/movies/{id}` | Update a movie |
| `PATCH` | `/api/movies/{id}` | Patch movie + details (JSON Patch) |
| `DELETE` | `/api/movies/{id}` | Delete a movie |
| `POST` | `/api/movies/{movieId}/actors/{actorId}` | Assign an actor |
| `GET`/`POST` | `/api/movies/{movieId}/reviews` | List / add reviews |

---

## Project structure

```
MovieApi-CA/
├─ MovieCore/            # entities, DTOs, domain contracts
├─ MovieData/            # EF Core context, repositories, UnitOfWork, mapping, migrations, seed
├─ MovieContracts/       # service interfaces + IServiceManager
├─ MovieServices/        # service implementations + business rules
├─ MovieServices.Tests/  # xUnit + NSubstitute tests
├─ MoviePresentation/    # controllers
├─ MovieApi/             # composition root, Program.cs, appsettings, MovieApi.http
├─ docs/
│  ├─ coding-steps.md    # development timeline — the ordered record of how the app was built
│  └─ adr/               # architecture decision records (0001–0004)
├─ CONTEXT.md            # domain glossary
└─ README.md
```

---

## Development process

This project was built with a **step-driven, LLM-supported process** designed to be reproducible: anyone can rebuild the application by following the same recorded steps in order.

The inputs and artifacts that make it repeatable:

1. **Design captured first.** Before any code, the design was written down: the LTU / Lexicon course brief (*Övning 6 — MovieAPI*), the domain glossary in [`CONTEXT.md`](CONTEXT.md), and the architecture decisions in [`docs/adr/`](docs/adr) (naming, many-to-many genre, error handling, the review trimmer). These are the single source of truth the rest of the process refers back to.

2. **A development timeline.** [`docs/coding-steps.md`](docs/coding-steps.md) is the chronological record of how the application was built, step by step. Each entry introduces one concept, ends in a **green build**, and closes with a **Verify** check and the **Commit** it produced — so the git log mirrors the timeline one-to-one. It began as a plan generated by an LLM assistant (Claude Code), but because each step was reviewed and revised as the code revealed gaps (see step 3), it ended up capturing the *actual* development process rather than an idealised plan: the order things were done, what broke, and how it was corrected.

3. **A review-then-implement loop.** Each step was first **examined for gaps and hidden details against the current codebase** — missing methods, wrong signatures, ordering or off-by-one issues, data needed to make a check testable — and the corrections folded back into `docs/coding-steps.md`. The code was then written to match, built, and run.

4. **Executable verification.** Each step has matching requests in [`MovieApi/MovieApi.http`](MovieApi/MovieApi.http), with the expected status codes noted inline, so behaviour can be checked the same way every time. The service layer is additionally covered by automated tests.

Because the guide, the request collection, the ADRs, and the domain glossary are all version-controlled alongside the code, the build is **reproducible**: re-reading the steps reproduces the same structure, the same decisions, and the same verifications.

> The LLM is used as an assistant within this loop — proposing steps, reviewing them against the actual code, and keeping the documentation in sync. Every change is built and verified before it is committed.
