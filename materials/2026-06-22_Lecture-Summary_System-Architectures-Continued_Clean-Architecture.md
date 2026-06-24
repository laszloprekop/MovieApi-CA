# Lecture Summary — Fullstack .NET · "Continued System Architectures / Clean Architecture"

**Date:** 2026-06-22 (Monday) · Morning session (~10:25–12:17, lunch break after)
**Teacher:** Michael Svensson (lead). Mats Lind posted the group lists in chat.
**Module:** ASP.NET Core Web API → System Architecture (continuation) → into Clean Architecture
**Source material:** lecture slide deck (`Clean-260622.pdf`), exercise brief (`Övning 6 – MovieAPI`), live transcript (SRT + VTT), Teams chat.

> **What this recording actually contains:** the *morning* half. It is mostly the six groups
> presenting the architecture comparison assignment, followed by Michael walking through the
> slide deck. The **hands-on Övning 6 walkthrough happens in the afternoon (from 13:10)**, which is
> *not* in this recording. The full written exercise is, though, and it is reconstructed in detail below.

---

## 1. TL;DR — the 60-second version

- Each group presented the **system-architecture comparison** assignment they submitted Friday. Almost every group gravitated to **Microservices** and **Vertical Slice**; Group 4 covered the "domain-centric family" (Layered, Clean, Onion, Hexagonal); Group 5 added **MVC**.
- Michael's takeaway: there is no single "right" architecture — you **pick the one that fits the problem**, and in a tech interview, naming the architecture (e.g. "we use Clean Architecture") already tells the interviewer a lot about how the codebase is shaped.
- He then walked the slide deck, layer by layer, anchoring everything on one principle: **the domain (core) sits in the middle and must not know about the outer technical layers** (web, database, frameworks).
- **The big new deliverable is Övning 6:** refactor your existing single-project Movie API (from Övning 3) into a **multi-project Clean Architecture solution** (repositories, Unit of Work, a service layer behind interfaces, paging, PATCH, business rules, error handling, tests). Posted today; presented/started in the afternoon.
- Housekeeping: you can now read the **other groups' submissions** (do it — six different takes on the same topic). Attendance check-ins still apply.

---

## 2. Actionables (do these)

### 2.1 Övning 6 — MovieAPI · Clean Architecture (the main one)

This builds directly on **Övning 3** (the single-project Movie API with `DbContext`, controllers, entities and DTOs all in one project). You now split that into layers. Full step-by-step plan is in **Section 5** below — read that section, it is the core of this document.

Goals the brief states explicitly:
- **Separation of Concerns (SoC)** — each layer has one responsibility.
- **Testability** — layers can be tested in isolation.
- **Scalability** — easier to extend with new features.
- **Reusability** — swap the database / UI, or reuse logic, without rewriting everything.

> ⚠️ **Naming clash to handle first:** you already have a `Movie` entity, which collides with the
> project name `Movie.*`. The brief's fix: rename the entity to `VideoMovie` (or similar), **or**
> drop the dot in project names. Pick one and be consistent before you start, or the compiler will
> fight you.

### 2.2 Read the other five groups' submissions

Michael unlocked all six group submissions specifically so you can compare. He framed it as a cheap way to see "six different explanations of the same architectures." Worth 20 minutes — different vocabulary and diagrams make the concepts stick.

### 2.3 Movie API status

Several people are mid-flight on the Movie API (Åsa: "I'm not done with Movie API"; Christofer: "probably today, maybe tomorrow"). Michael reassured the class that the Pluralsight Clean Architecture lab and the group assignment were **complements**, not meant to crowd out the main Movie API work. Don't let the side tasks fully displace the API.

### 2.4 Attendance / admin (unchanged, but it bites)

Daily check-ins at **08:00–08:15, 13:00–13:15, 16:00–17:00** via Teams/forms. Absence must be reported **before 08:00** by email to `infoAF@lexicon.se` **and** your teacher (`michael.svensson@lexicon.se`). Missing a check-in without prior notice = invalid absence reported to Arbetsförmedlingen. (Source: the attendance/absence PDFs in the project — not part of the lecture itself.)

---

## 3. The teaching content — architectures, junior-developer friendly

Michael's organising idea for the whole deck: **architectures are different answers to one question — "where do I put my code, and which parts are allowed to depend on which other parts?"** Two themes recur:

1. **The dependency rule** (the heart of Clean/Onion/Hexagonal): dependencies point *inward*, toward the domain. The domain knows nothing about the database or the web framework.
2. **Organise by layer vs organise by feature** (Layered/Clean vs Vertical Slice): two orthogonal ways to slice the same system.

### 3.1 The "dependency direction" mental model (most important picture)

```
            OUTER (volatile, technical)                INNER (stable, business)
   ┌─────────────────────────────────────┐
   │  Presentation  (controllers, HTTP)  │  ── depends on ──►  Application
   │  Infrastructure (EF Core, email,    │  ── depends on ──►  Application / Domain
   │                  queues, logging)   │
   │        Application (use cases,       │  ── depends on ──►  Domain
   │         service logic, interfaces)   │
   │              Domain (entities,       │  ── depends on ──►  (nothing)
   │               business rules)        │
   └─────────────────────────────────────┘
   Arrows ALWAYS point inward. The Domain has zero outward references.
```

The trick that makes the arrows point inward even though *data flows outward to the database*: the inner layer **defines an interface** (e.g. `IMovieRepository`) and the outer layer **implements it**. This is the **Dependency Inversion Principle**, and it is exactly what Övning 6 makes you build. (More on the subtlety in Section 7.)

### 3.2 Clean Architecture (the deck's headline)

- **Idea:** domain-centric. A **core** holds the domain (entities, business rules) and the use cases; outer rings hold web, databases and other technical detail. The core stays **independent of frameworks and infrastructure**, which makes the system more testable and easier to change over time.
- **The deck's layer inventory** (useful as a "where does X go" cheat-sheet):
  - **Domain:** Domain Entities, Aggregates, Value Objects, Domain Events, Enumerations, Constants.
  - **Application:** Abstractions, Contracts, **Ports**, **Interfaces**, Business Services, Commands and Queries, Application Exceptions, **DTOs**, Request/Response models, entity→DTO mappers, Validators, Behaviors, Specifications.
  - **Infrastructure:** Authentication/Identity, File/Object Storage, **Message Queue Storage**, Third-party Services, **Email & Notification Services**, **Logging Services**, Payment Services, Social Logins.
  - **Persistence:** Data Context, **Repositories**, Data Migrations, Data Seeding, In-Memory Caching, Distributed Caching (Redis, Memcached).
  - **Presentation:** Web Pages, Web Components, **Web APIs**, **Controllers**, Views, **Middleware**, **Filters**, Attributes, View Models, Style Sheets, JS files.
- **Use it when:** domain logic is important and worth protecting; you want to swap database tech or UI without touching the core; you prioritise testability and long-term maintainability.

> Michael's interactive moment: he pointed at "Notification Services" in the Infrastructure ring and
> asked what it reminds you of — answer from the room: **SignalR / push notifications / signals**.
> And "Logging Services" → in production you don't surface raw errors to the user; you **log
> internally** and return clean error responses. That's the motivation for the error-handling part of
> Övning 6 (Del 9).

### 3.3 Onion Architecture

- **Idea:** essentially the same as Clean — domain in the centre, technical dependencies further out, everything pointing toward the core; the innermost layer must not know the outer layers.
- **Deck verdict:** "almost the same as Clean in practice." Strength: a very clear, circular **dependency model**. Best for **domain-oriented systems with a lot of business logic**.

### 3.4 Vertical Slice Architecture

- **Idea:** organise by **feature / use case** instead of by technical layer. Each *slice* contains everything that one feature needs — endpoint, request/response, handler, validation, data access — sometimes in a single file.
- **Example folder layout from the deck:**
  - `Features/Orders/CreateOrder`
  - `Features/Orders/GetOrderById`
  - `Features/Customers/SearchCustomers`
- **Why people like it (raised by the groups):** features can be **developed in parallel**; changing one slice doesn't ripple into the others because slices are loosely coupled.
- **Cost (deck):** "requires discipline around boundaries" — because each slice does its own thing, you can end up **repeating code** across slices, and there is less of a single enforced standard.
- Michael's analogy: think **pizza slices** — one slice is the whole vertical stack (crust → topping) for one piece, rather than one horizontal layer shared across the whole pizza.

> **Worth knowing:** Vertical Slice is about *code organisation*, not *deployment*. A chat comment
> ("vertical slice is usually a kind of monolith") is fair — you almost always deploy a vertical-slice
> codebase as a single app. So "Vertical Slice vs Microservices" is not really apples-to-apples:
> Microservices is a *deployment* decision, Vertical Slice is a *file-organisation* decision. You can
> even use vertical slicing *inside* a microservice.

### 3.5 Layered Architecture

- **Idea:** the classic split — API/UI → Service/Application → Domain → Infrastructure. Simple and very familiar.
- **Typical layout (deck):** `API/Controllers`, `Application/Services`, `Domain/Models`, `Infrastructure/Repositories + EF Core + external integrations`.
- **Strength:** simple and well-known; a good starting point for small-to-medium APIs.
- **Weakness (deck):** "can become heavy and **tightly coupled**"; gets awkward as the app grows and many features cut across the same layers.
- Chat colour: it was "very common in the old days," and a student noted it "mirrors the OSI model well" — meaning the *idea* of stacked layers where each talks mainly to its neighbour. (That's an analogy to the layering *principle*, not a literal mapping to networking's 7 layers.)

### 3.6 Modular Monolith

- **Idea:** still **one deployed application** (a monolith), but internally divided into **modules** with clear boundaries, usually around business areas (e.g. `Users`, `Payments`). Each module can have its own domain/application/infrastructure, but the whole thing runs as a single app.
- **Use it when:** you want to avoid microservices complexity, you have several business domains that should be clearly separated, and you might want to grow toward service-orientation later.
- **Deck verdict:** often a strong choice for larger Web API projects that don't yet need to be distributed as microservices. Michael noted your Movie API is starting to resemble this as it grows into layers.

### 3.7 Hexagonal Architecture (Ports & Adapters)

- **Idea:** the core exposes **ports** (interfaces) that the outside world connects to via **adapters** (implementations). In practice very close to Clean Architecture: the domain is protected, and infrastructure (EF Core, REST, messaging, external APIs) becomes **swappable adapters**.
- **In ASP.NET Core terms:** controllers/endpoints are **inbound adapters** (driving the use cases), while email, queues and databases are **outbound adapters** (driven by the core).
- Chat colour (good summary by a student): the name comes from the **hexagon shape in the diagrams**, used to show the core talking to many external components through ports (interfaces) and adapters (implementations). **The six sides are pedagogical — there is no rule that a system must have six parts.**

### 3.8 MVC (added by Group 5)

- **Idea:** **Model–View–Controller.** Model = your data/class; View = usually HTML + a framework; Controller = handles input and coordinates the two. Separating the three lets you swap any one of them easily.
- **Strength:** so simple you can use it even in a tiny monolith; flexible and long-established.
- **Note for *your* context:** an ASP.NET Core **Web API** uses MVC's controller concept but typically has **no server-rendered Views** — it returns data (JSON), not HTML. So in your Movie API the "V" is effectively the client/consumer, not a Razor view. Keep that distinction when you read MVC material.

### 3.9 Microservices (the one everyone picked)

- **Idea:** split the application into **small, independently deployable services**, each ideally owning its own database (sometimes even its own language/stack), built and run by separate teams. If one service goes down, the others keep running.
- **How they talk:** over the network via **HTTP/REST APIs** and/or a **message broker / message queue** (RabbitMQ, Kafka). Often fronted by an **API Gateway** that routes incoming requests to the right service.
- **Strengths:** strong isolation — each service can be changed, scaled and deployed independently; easy to scale a single service up/down (e.g. a container under load).
- **Costs (raised by the groups):** much more **setup and operational overhead** — you need the gateway, containers (Docker/Kubernetes), monitoring, logging, and a **CI/CD pipeline**; if the gateway is misconfigured, "the whole app falls apart" because it funnels all traffic; network calls between services introduce new failure modes.

> Michael's reality check: he asked, only half-joking, **"why does everyone choose
> microservices?"** Marcus's honest answer: "it's in every job ad." Michael's point: at a large
> company (his example: Volvo) you very likely *will* run microservices — but for a small Web API like
> yours, microservices is usually **overkill**. Vertical Slice is also "coming more and more, and many
> are moving away from Clean Architecture" — but again, choose by fit, not by hype.

### 3.10 The deck's comparison table (consolidated)

| Architecture | Strength | Weakness | Best for |
|---|---|---|---|
| **Layered** | Simple, well-known | Can get heavy & tightly coupled | Small–medium APIs |
| **Clean** | Testable, domain-focused | More structure, more projects | Business-critical systems |
| **Onion** | Clear circular dependency model | Almost the same as Clean in practice | Domain-oriented systems |
| **Vertical Slice** | Feature-focused, practical | Needs discipline around boundaries | APIs with many use cases |
| **Modular Monolith** | Good module boundaries without distributed complexity | Requires good module design | Larger systems kept simple |
| **Hexagonal** | Flexible, technology-neutral | Can feel abstract | Systems with many integrations |

---

## 4. Group presentations — who covered what

Tech glitches scrambled the order; here is the substance, attributed as cleanly as the captions allow.

- **Group 1** (Simon Sundqvist, Edvin Skogsholm Sanne, Konstantinos Konstantinidis, **Laszlo Prekop**, Marcus Pettersson) — covered **Layered, Vertical Slice, Microservices**, with code examples for `GET`. Presented by Edvin. Their narrative: **coupling decreases as you move Layered → Vertical Slice → Microservices**, with Microservices having the highest separation (suited to larger teams/systems). Michael noted he believes **László built the base of the comparison table** in the cloud doc, which the group then re-edited for readability — and explicitly praised this as the *correct* use of AI tooling: lean on it for **compilations/summaries**, then refine.
- **Group 2** (Simon Nordstrand, Martin Leo, Åsa Petersson, Mattias Jönsson, Anton Roback) — **Microservices + Vertical Slice** (some overlap with Group 1, by design). Presented mainly by Martin (mic was very quiet). Highlighted cloud-hosted microservices (AWS, containers, Kubernetes), per-service scaling, and the operational burden: monitoring, logging, CI/CD, and the gateway as a single critical path.
- **Group 5** (Johanna Sewring, **Amos Persson**, Adam Matthews, Lars Karlqvist, Alexander Stauch) — **MVC** then **Microservices**. Showed code snippets: a **message processor interface** (meant to bind to RabbitMQ/Kafka) and a **background worker** that consumes orders from the queue and waits on a **cancellation token**. Their framing of the core idea: independent services listening on the same message queue can cooperate **without knowing about each other**.
- **Group 6** (Bahador Nezakati, **Amer Mauweyah**, Christofer Nyström, Anton Bergmark, Link Häggman) — **Microservices + Vertical Slice**. Amer demoed slices using **`GetMovies` / `AddMovie`** (they reused the Movie domain on purpose). Reinforced: each microservice is almost its own project (own domain/application/infrastructure), can differ internally, and what matters is a **standardised way to communicate**; API Gateway routes to the right service.
- **Group 4** (**Anders Hansen-Haug**, Dragos Cuciureanu, Christer Vadman, Joel Edegran, Sami Ahlfors) — the "domain-centric family": **Layered, Clean, Onion, Hexagonal**, ending with a comparison. Dragos's honest line — parts of it "felt like rocket science"; Michael's reply: take it **step by step ("bit för bit")**, it will settle.
- **Group 3** (Jonatan Streith, **Niklas Söderberg**, Paulina Ferrada, Peter Broman, Yang Li) — delayed by a missing presenter; presented after the break via **Google Docs** (Niklas shared; Paulina had the link). Discussion centred on their API/service work and refactoring the controllers.

Michael's meta-point about the whole exercise: six groups, **little cross-coordination**, yet you collectively explained Microservices "three different ways and all of them were right." Presenting under short notice ("oförberedd," unprepared) is itself the skill being practised — in a real job you'll often have to summarise where you are at a quick stand-up/Scrum.

---

## 5. Övning 6 — full step-by-step plan

> This reconstructs the written brief into an ordered build. The brief is intentionally iterative:
> **Parts 1–3 first get a layered split working, then Parts 8–9 split further into the full Clean
> Architecture shape.** Don't try to build the final 7-project structure on day one.

### Target project structure & responsibilities

| Project | Responsibility |
|---|---|
| **Movie.API** | Startup & configuration (composition root). Registers services into DI (AutoMapper, `IServiceManager`, etc.), configures & starts the app, owns the request pipeline (middleware). |
| **Movie.Presentation** | Controllers + exposing the API endpoints. All controllers move here. Handles HTTP request/response. |
| **Movie.Services** | Business logic. Implements the services; talks to **Unit of Work** and repositories via `IUnitOfWork`; does mapping. |
| **Movie.Services.Contracts** (a.k.a. Movie.Contracts) | Interfaces for the service layer (`IMovieService`, `IActorService`, …) and an umbrella `IServiceManager`. |
| **Movie.Core** | Merge of Domain.Models + Domain.Contracts + shared DTOs. Holds **entities** (`Movie`, `Actor`, …), **DTOs** (request/response), and a `DomainContracts` folder with **repository + Unit of Work interfaces**. |
| **Movie.Data** | Repository implementations, `UnitOfWork`, EF Core `DbContext` configuration, AutoMapper profiles, migrations, seeding. |

### Phase A — Parts 1–3: get a layered split compiling

**Del 1 — new project structure**
1. Create (or extend) the solution with **three** projects to start: `MovieApi`, `Movie.Core`, `Movie.Data`.
2. Project references (transitional):
   - `MovieApi` → references `Movie.Data`
   - `Movie.Data` → references `Movie.Core`
3. Move **entities, DTOs and interfaces** from Övning 3 into `Movie.Core`.
4. Move **database logic and seeding** into `Movie.Data`.
5. Build and confirm it still runs exactly as before.

**Del 2 — Repositories + Unit of Work**
1. In `Movie.Core`, create a `DomainContracts` folder with `IMovieRepository` (the brief stresses this is a *starting point* you'll keep editing):
   ```csharp
   public interface IMovieRepository
   {
       Task<IEnumerable<Movie>> GetAllAsync();
       Task<Movie> GetAsync(int id);
       Task<bool> AnyAsync(int id);
       void Add(Movie movie);
       void Update(Movie movie);
       void Remove(Movie movie);
   }
   ```
2. In `Movie.Data`, create a `Repositories` folder. Implement `MovieRepository : IMovieRepository`, **inject `MovieContext`** in the constructor, implement the methods.
3. Repeat for the other entity repositories (`IActorRepository`/`ActorRepository`, `IReviewRepository`/`ReviewRepository`, …).
4. Create `IUnitOfWork` in `Movie.Core.DomainContracts` (example — adapt as needed):
   ```csharp
   public interface IUnitOfWork
   {
       IMovieRepository Movies { get; }
       IReviewRepository Reviews { get; }
       IActorRepository Actors { get; }
       Task CompleteAsync();
   }
   ```
5. Implement `UnitOfWork : IUnitOfWork` in `Movie.Data.Repositories`. **Inject `MovieContext`**; implement the repository properties and `CompleteAsync()` (which calls `SaveChangesAsync` on the context).
6. Register it in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
   ```
   **The brief asks: why `AddScoped`?** → Because it matches EF Core's `DbContext` lifetime: one `DbContext` (and therefore one `UnitOfWork` sharing it) **per HTTP request**. `AddSingleton` would trap a scoped `DbContext` inside a singleton (a **captive-dependency** bug — threading issues, stale data). `AddTransient` would spin up several `UnitOfWork`/`DbContext` instances per request, breaking the "one transaction per request" guarantee the Unit of Work is supposed to give.

**Del 3 — integrate `IUnitOfWork` into controllers**
1. Swap the controllers' direct `DbContext` use for `IUnitOfWork`. Confirm everything still behaves as before.

### Phase B — Parts 4–7: features on the new foundation

**Del 4 — Paging & metadata**
1. Every list endpoint (movies, actors, …) takes query parameters:
   - `pageSize` (default **10**, max **100**) and `page` (default **1**).
   - Client passes them as a query string, e.g. `?pageSize=20&page=3`.
     > ⚠️ The brief prints `?pageSize=20?page=3` — that second `?` is a typo; query params after the
     > first are joined with `&`, so the correct form is `?pageSize=20&page=3`.
   - If the client asks for more than 100 (e.g. `pageSize=110`), **clamp to 100**.
   - If no `pageSize` is given, use the default (10).
   - **Tip from the brief:** wrap the paging params in a single object (e.g. `PaginationParameters { PageSize, PageNumber }`) instead of binding many separate values. **Because the model binder is reading a complex object from the query string, you must put `[FromQuery]` on that object parameter** or binding silently fails.
2. Return metadata alongside the DTOs: `totalItems`, `currentPage`, `totalPages`, `pageSize`. Put it either in the response **body** (a `{ "data": [...], "meta": {...} }` envelope) **or** in a response **header** — the brief suggests an `X-Pagination` header (same idea as the `Location` header you've used: key `X-Pagination`, value = the metadata).
3. Create a strongly-typed `PagedResult<T>` model so you have a hard type to work with.

**Del 5 — Normalisation (if not already done in Övning 3)**
1. Move **genre into its own table.** Relationship: **one movie has one genre; a genre can belong to many movies** (one-to-many, `Genre 1 — * Movie`).
   > Note: this is a deliberate simplification — real catalogues often model **many-to-many** (a film
   > has several genres). Build what the brief asks (1-to-many) but know why it's simplified.
2. Seed a few genres (**`Documentary` is mandatory**).
3. When creating a `Movie`, it must be tied to an **existing** genre.
4. If the genre doesn't exist, return an error as a **`ProblemDetails`** explaining why.

**Del 6 — Business rules** (implement these; invent more if you like)
1. A movie may have at most **10 reviews**.
2. An actor may not be assigned to the same movie twice.
3. Budget may not be negative.
4. A movie older than 20 years may have at most **5 reviews**. *(Optional extra: a background service that runs every 10 minutes and trims the oldest reviews on films that have just turned 20 — see the note below.)*
5. A **Documentary** may not have more than 10 actors and not a budget over 1,000,000.
6. No two movies may share the same name.
   > Rule 4's "service every 10 minutes" = a **hosted background worker** (`BackgroundService` /
   > `IHostedService`) — the same "background worker consuming on a loop" pattern Group 5 demoed for
   > microservices. It runs independently of HTTP requests.

**Del 7 — PATCH for Movie**
1. It must be possible to **partially update both a `Movie` and its `MovieDetails` in the same endpoint**.
2. Implement with a **`PatchDocument`** (in ASP.NET Core: `JsonPatchDocument<T>`).
   > ⚠️ Gotcha: `JsonPatchDocument` historically needs the
   > **`Microsoft.AspNetCore.Mvc.NewtonsoftJson`** package and `AddNewtonsoftJson()` (it's built on
   > Newtonsoft.Json, not System.Text.Json). On newer .NET there is System.Text.Json patch support,
   > but if your `[FromBody] JsonPatchDocument<...>` won't bind, the missing Newtonsoft package is the
   > usual cause. Also remember to call `patchDoc.ApplyTo(target, ModelState)` and re-validate.

### Phase C — Parts 8–10: the full Clean Architecture split

**Del 8 — Service layer** (the yellow projects in the brief's diagram)
1. Add the projects: `Movie.Services` (business logic, mapping, service + `ServiceManager` implementations), `Movie.Services.Contracts` (service interfaces), `Movie.Presentation` (controllers / API exposure).
2. Update project references to match the diagram — **the brief deliberately gives no help here**; the intended shape is:
   - `Movie.API` → `Movie.Presentation`, `Movie.Services`, `Movie.Data` (composition root)
   - `Movie.Presentation` → `Movie.Services.Contracts`
   - `Movie.Services` → `Movie.Core`, `Movie.Services.Contracts`
   - `Movie.Services.Contracts` → `Movie.Core`
   - `Movie.Data` → `Movie.Core`
   - `Movie.Core` → *(nothing)*
3. Implement the service classes (error handling comes in Del 9).
4. Extract interfaces (or write the interface first — either order).
5. Register everything in DI.
6. Use the services from the controllers.
7. **Hard requirement:** no controller may reference `DbContext`, `UnitOfWork` or AutoMapper directly — controllers talk **only** to `IServiceManager`.
8. Confirm behaviour is unchanged.

   **Project dependency picture (target):**
   ```
   Movie.API ──► Movie.Presentation ──► Movie.Services.Contracts ──► Movie.Core
       │                                                               ▲   ▲
       ├──► Movie.Services ──► Movie.Services.Contracts ───────────────┘   │
       │          └──► Movie.Core ─────────────────────────────────────────┤
       └──► Movie.Data ──► Movie.Core ─────────────────────────────────────┘
   Movie.Core references nothing. All arrows ultimately point at Movie.Core.
   ```

**Del 9 — Error handling** (now harder, because it happens deeper in the stack)
1. You **no longer have `NotFound()` / `BadRequest()`** from `ControllerBase` available in the service layer (services aren't controllers).
2. Handle errors either with a **custom return type** *or* as a dedicated **middleware component** — try both on different controllers if you want.
3. Keep returning correct status codes: `404 Not Found`, `400 Bad Request`, `406 Not Acceptable`, etc.
4. Keep using **`ProblemDetails`** for error bodies.

**Del 10 — Tests & reflection**
1. Write **at least 3 unit tests** for your service layer.
2. Reflect on: how this differs from earlier exercises; the benefits of multi-layer architecture; how you'd use it in a real project.

**Del 11 — Free experimentation**
Add anything you've seen in e-learning or class. Optional.

---

## 6. Q&A and discussion highlights

### From the teacher (questions he posed)
- **"Why `AddScoped` for the Unit of Work?"** → see Del 2.6 above (matches `DbContext` request lifetime; avoids captive dependencies).
- **"Why does everyone choose Microservices?"** → because it's in every job ad (Marcus). Michael's nuance: expected at big companies, usually overkill for a small Web API.
- **"What does 'Notification Services' / 'Logging Services' in the Infrastructure ring remind you of?"** → push notifications / SignalR / signals; and "in production, log internally, don't leak raw errors to the client."

### From the students (and the Teams chat)
- **"Is Vertical Slice basically a kind of monolith?"** (Christofer) → Yes, usually deployed as a monolith; it's about *organising by feature*, not about deployment. (See the note in 3.4.)
- **"Hexagonal looks like Clean Architecture with extra steps."** (Jonatan) → Close in spirit. Historically it's the other way round: **Hexagonal (Cockburn, 2005) and Onion (Palermo, 2008) predate Clean Architecture (Martin, 2012)**, which synthesised them. So Clean is more like "Hexagonal/Onion made more prescriptive," not Hexagonal being a derivative of Clean. Either way, same dependency-rule idea.
- **Hexagonal name** (Lars, good summary) → the hexagon is a teaching symbol for "core ↔ many external components via ports & adapters"; there's no rule of six.
- **"Layered was very common before; it mirrors the OSI model."** (Peter) → an analogy to the layering *principle* (adjacent-layer communication), not a literal 7-layer mapping.
- **"I worked with SOA, which I suspect evolved into microservice architecture."** (Peter) → broadly true: **microservices grew out of SOA (Service-Oriented Architecture)**, sharpening it toward small, independently deployable, single-responsibility services with lightweight communication.
- **Splitting layers into separate *projects* vs one project** (Peter, Niklas) → Peter: "refactoring into separate projects per layer" = **decoupling**. Niklas: you *can* do it all in one project, but the Pluralsight-lab way (separate projects) is better **if you might later move a part to something other than C#** — i.e. project boundaries buy you portability and enforce decoupling at compile time. (This is precisely the SoC/reusability goal of Övning 6.)
- **PATCH** (Peter / Niklas / Lars):
  - Peter: "PATCH feels like a fairly new method, works well with JSON-Patch." → It's an **HTTP** method (not HTML), standardised in **RFC 5789 (2010)** — newer than GET/POST/PUT/DELETE but well established now.
  - Niklas: "many use POST even for PATCH and PUT, e.g. Reddit." → True; lots of APIs overload `POST`. Pragmatic, but not strictly RESTful.
  - Lars's example (correct): **`PUT /users/42`** with a full body **replaces** the resource; **`PATCH /users/42`** with `{ "email": ... }` only **changes that field**. That's the PUT-vs-PATCH distinction Övning 6 Del 7 is testing.
  - Peter's `git patch` analogy: a teammate hasn't pushed their changes, but you can still consume them via a patch — a nice intuition for "a patch = a description of changes," which is also what **JSON Patch (RFC 6902)** is (a list of operations like `replace`/`add`/`remove`).
- **Best architecture?** (Peter) → "the one best suited to solve the problem / the integration we're discussing." Michael agreed — fit beats fashion. Peter also shared a war story: a greenfield project replacing an old university system adopted an architecture that "caused big problems," not because the architecture was wrong but because of how it was applied/understood — a good reminder that misapplied architecture hurts more than a simpler well-applied one.

---

## 7. Corrections, simplifications & blind spots (read this carefully)

These are places where the source material or the live discussion is slightly loose. None of this is "wrong-and-bad"; it's the nuance that's easy to miss live, especially in a second language.

1. **Where do repository interfaces "belong"? (the most useful nuance for you.)**
   The deck's Clean diagram puts **Interfaces/Ports/Contracts in the Application ring**. But **Övning 6 puts the repository + Unit-of-Work interfaces in `Movie.Core` (Domain)**, under `DomainContracts`. Both are common; sources disagree on whether repository abstractions live in *Domain* or *Application*. **The invariant — and the only thing that matters for Dependency Inversion — is that the interface lives in an *inner* layer and the implementation in an *outer* layer, so the compile-time dependency points inward.** Don't get hung up on Domain-vs-Application placement; get the *direction* right.

2. **"Movie.API references Movie.Data" looks like it violates the dependency rule — it doesn't.**
   In Del 1 (and again at the composition root in Del 8) the API project references `Movie.Data`. That's fine: the **composition root** is the *one* place allowed to know about concretes, because it's where you wire `IUnitOfWork → UnitOfWork`. The rule "don't depend on infrastructure" applies to your **business layers** (Domain, Application/Services), not to the startup project whose entire job is wiring. A cleaner pattern many books use: each layer exposes its own `AddXxx(this IServiceCollection)` extension method, so `Program.cs` calls `builder.Services.AddData()` without the API touching `Movie.Data`'s internal classes. Worth doing in Del 8.

3. **Del 1's three-project shape is *Layered*, not yet *Clean*.**
   With `MovieApi → Movie.Data → Movie.Core`, the API sits on top of infrastructure — that's a layered architecture. The **Clean** shape only emerges in Del 8 once `Movie.Services`/`Movie.Services.Contracts`/`Movie.Presentation` are added and controllers depend on *interfaces* (`IServiceManager`) rather than on `Movie.Data`. So the exercise literally walks you Layered → Clean. Knowing that makes the early steps less confusing.

4. **`IServiceManager` is a specific (opinionated) pattern, not a universal Clean Architecture rule.**
   The `IServiceManager` + `ServiceManager` (lazily instantiating each service) facade comes from the popular *"Ultimate ASP.NET Core Web API"* (Code Maze) style. It's a fine pattern, but it's a *convention this course adopts*, not something every Clean Architecture codebase has. Don't be surprised if other Clean projects inject `IMovieService` directly instead of going through a manager.

5. **`?pageSize=20?page=3` is malformed** — use `&` between query params (`?pageSize=20&page=3`). (Del 4.)

6. **`JsonPatchDocument` needs Newtonsoft** in most setups — the #1 reason PATCH "doesn't bind." (Del 7.)

7. **RabbitMQ and Kafka are not interchangeable**, even though they were named in one breath. **RabbitMQ** is a traditional **message broker / queue** (smart broker, work distributed and acknowledged, messages typically consumed once). **Kafka** is a distributed **append-only commit log / streaming platform** (messages retained, consumers track their own offset, great for replay and high-throughput event streams). Both enable async service-to-service communication, but with different guarantees and use cases. ("Message broker" was also transcribed as "Merced broker" in the captions — it's *message broker*.)

8. **PATCH is HTTP, not HTML** (it transports *to* an HTTP endpoint; HTML is the markup language). Minor slip in chat.

9. **Microservices "each service its own database/language" is the ideal, not a requirement.** Many real systems share databases across services early on (an anti-pattern called the *shared database*, but extremely common in practice). Treat the textbook description as the target state.

10. **MVC's "controller sits between model and view and handles communication"** is a fair shorthand, but classically the **View observes the Model directly**; the Controller handles *input* and updates the Model. And again — your **Web API has no Views**, so the mental model transfers only partially. (See 3.8.)

11. **Genre as one-to-many** is a modelling simplification (real films are usually many-to-many on genre). Build what Del 5 asks, but file the limitation away.

---

## 8. Vocabulary glossary (new / reinforced terms)

| Term | Plain-English meaning |
|---|---|
| **System architecture** | The high-level shape of a codebase: which parts exist and which may depend on which. |
| **Clean Architecture** | Domain-centric layering; the core (domain + use cases) knows nothing about web/DB/frameworks. |
| **Onion Architecture** | Same idea as Clean, drawn as concentric circles with the domain at the centre. |
| **Hexagonal Architecture (Ports & Adapters)** | Core exposes **ports** (interfaces); the outside connects via **adapters** (implementations). |
| **Port** | An interface the core exposes/needs (e.g. `IMovieRepository`). |
| **Adapter** | A concrete implementation that plugs into a port (e.g. `MovieRepository` using EF Core). |
| **Vertical Slice Architecture** | Organise code **by feature** (each slice = full stack for one use case) rather than by technical layer. |
| **Layered Architecture** | Classic horizontal split: API → Service → Domain → Infrastructure. |
| **Modular Monolith** | One deployed app, internally split into clearly bounded modules (often per business area). |
| **MVC** | Model–View–Controller; separates data, presentation and input handling. |
| **Microservices** | Many small, independently deployable services, each owning its slice of the system. |
| **API Gateway** | Single entry point that routes/forwards client requests to the right microservice. |
| **Message broker / message queue** | Middleware that passes messages between services asynchronously (RabbitMQ). |
| **Kafka** | Distributed, retained, append-only event log / streaming platform (different from a queue). |
| **Background worker** | A `BackgroundService`/`IHostedService` that runs a loop independent of HTTP requests (e.g. consuming a queue, or a periodic cleanup). |
| **Cancellation token** | A signal that asks an async/looping operation to stop gracefully (used by background workers). |
| **SoC (Separation of Concerns)** | Each part of the code has one clear responsibility. |
| **Coupling / Decoupling** | How much one part depends on another; decoupling = reducing those dependencies. |
| **Dependency Inversion Principle (DIP)** | Depend on interfaces, not concretes; define the interface in the inner layer, implement it in the outer layer — so dependencies point inward. |
| **Composition root** | The single startup place (here `Movie.API`/`Program.cs`) allowed to know concretes and wire interfaces to implementations. |
| **Repository** | An abstraction over data access for one entity type (`IMovieRepository`). |
| **Unit of Work** | Coordinates several repositories and commits them together in one transaction (`CompleteAsync()`). |
| **DTO (Data Transfer Object)** | A shaping object for what goes in/out over the API, decoupled from the entity. |
| **DI lifetime — Scoped** | One instance per HTTP request (matches `DbContext`; correct for `UnitOfWork`). |
| **DI lifetime — Singleton** | One instance for the whole app (dangerous if it captures a scoped dependency = captive dependency). |
| **DI lifetime — Transient** | A fresh instance every time it's requested. |
| **Captive dependency** | A long-lived service holding a shorter-lived one (e.g. singleton holding a scoped `DbContext`) → bugs. |
| **PUT** | Replace the entire resource. |
| **PATCH** | Apply a **partial** update to a resource (HTTP method, RFC 5789). |
| **JSON Patch (`JsonPatchDocument`)** | A list of operations (`add`/`remove`/`replace`/…) describing a partial change (RFC 6902). |
| **ProblemDetails** | The standard structured error body for HTTP APIs (RFC 7807/9457). |
| **Paging metadata** | `totalItems`, `currentPage`, `totalPages`, `pageSize` — info about a paginated result. |
| **`[FromQuery]`** | Tells the model binder to build a parameter (incl. a complex object) from the query string. |
| **`X-Pagination` header** | A custom response header carrying paging metadata (instead of putting it in the body). |
| **Normalisation** | Splitting data into related tables to remove duplication (e.g. genre → its own table). |
| **SOA** | Service-Oriented Architecture; the older paradigm microservices evolved from. |
| **CI/CD pipeline** | Automated build/test/deploy; essential operational glue for microservices. |
| **Containers / Kubernetes** | Packaging (Docker) and orchestration tech commonly used to run microservices. |

---

## 9. Background / housekeeping

- **Format of the day:** group presentations of the architecture comparison (submitted last Friday), a short break (the running "Kafka vs *kaffe* / *Kafkapaus*" joke), then Michael's slide walkthrough. **Övning 6 was posted today; presentation/work starts in the afternoon (13:10).**
- **On using AI well:** Michael explicitly endorsed using AI tools to *compile/summarise* (he guessed parts of a comparison table were AI-assisted and said that's "exactly how you should work today") — the skill is using the tool correctly for the right job, then refining. (Worth pairing with your own goal of also keeping the *unassisted* muscle sharp for interviews — the architectures above are exactly the kind of thing an interviewer will ask you to whiteboard from a blank page.)
- **Reading list of the day:** the other five groups' submissions (now unlocked).

---

*Prepared as an English study companion to the 2026-06-22 session. Speaker attribution comes from the
captioned transcript; where the captions were too garbled to attribute confidently, the point is stated
without a name. Architecture and exercise details are taken from the slide deck and the Övning 6 brief.*
