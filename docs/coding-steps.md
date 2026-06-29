# Coding Steps: MovieAPI — Clean Architecture (Övning 6)

**Stack:** C# / .NET 10 · ASP.NET Core Web API · EF Core 10 (SQL Server) · AutoMapper · xUnit + NSubstitute
**Design plan:** `materials/Övning 6 MovieAPI Fortsättningen!.pdf` + `CONTEXT.md` + `docs/adr/0001–0004`
**Generated:** 2026-06-24

## Architecture at a glance

- **MovieCore** — entities (`Movie`, `MovieDetails`, `Genre`, `Review`, `Actor`), DTOs, and a `DomainContracts` folder (repository + Unit-of-Work interfaces). References **nothing**.
- **MovieData** — EF Core `MovieContext`, repository + `UnitOfWork` implementations, AutoMapper profiles, migrations, seed. → MovieCore.
- **MovieContracts** — service interfaces (`IMovieService`, …) + the `IServiceManager` facade. → MovieCore.
- **MovieServices** — service implementations + `ServiceManager`; all business rules + mapping via `IMapper`. → MovieCore, MovieContracts.
- **MoviePresentation** — controllers; they talk **only** to `IServiceManager`. → MovieContracts.
- **MovieApi** — composition root: DI wiring, `IExceptionHandler`/`ProblemDetails`, NewtonsoftJson, request pipeline. → MoviePresentation, MovieServices, MovieData.

**Decisions baked in:** Genre is **many-to-many** (ADR 0002); errors via **domain exceptions + `IExceptionHandler`** (ADR 0003); paging metadata in an **`X-Pagination` header**; **AutoMapper**; all 6 business rules enforced in the **service layer only**; PATCH via a **flat `MoviePatchDto`**; the review trimmer is an **idempotent `BackgroundService`** (ADR 0004, Phase 2).

> **Naming (ADR 0001):** project names drop the dot (`MovieApi`, `MovieCore`, …) so the namespace never collides with the `Movie` type.

---

## Phase 0 — Walking Skeleton

> **Walking skeleton:** the thinnest end-to-end slice proving every layer can talk to the next. Here that means: the new **3-project layered solution** compiles and serves your existing Övning 3 endpoints *exactly as before* — no new behaviour yet. This is brief **Del 1**.
>
> Every step verifies: **the app boots and `GET /api/movies` still works.** Every commit is `chore`.

### Step 1: Create the solution and three projects

> **New concept: multi-project solution.** A C# solution (`.sln`) groups several projects (`.csproj`), each compiling to its own assembly. Project references are enforced at **compile time** — this is how Clean Architecture stops the domain from depending on infrastructure.

Start the new structure in this repo (`MovieApi-CA`). The dependency direction is `MovieApi → MovieData → MovieCore`.

```bash
# repo root (MovieApi-CA)
dotnet new sln -n MovieApi
dotnet new webapi   -n MovieApi   -f net10.0 --use-controllers
dotnet new classlib -n MovieCore  -f net10.0
dotnet new classlib -n MovieData  -f net10.0

dotnet sln add MovieApi MovieCore MovieData
dotnet add MovieData/MovieData.csproj reference MovieCore/MovieCore.csproj
dotnet add MovieApi/MovieApi.csproj   reference MovieData/MovieData.csproj
```

**Verify:** `dotnet build` — three projects compile, zero errors.
**Commit:** `chore(solution): scaffold MovieApi/MovieCore/MovieData layered solution`

### Step 2: Bring your Övning 3 source into MovieApi

Copy your working Övning 3 files (from your `MovieApi` Övning-3 project) into the new `MovieApi/` project — `Controllers/`, `DTOs/`, `Models/`, `MovieContext.cs`, `Extensions/SeedDataExtensions.cs`, `Program.cs`, `appsettings*.json`. Add the EF Core packages it needs to `MovieData` (where the context will live next).

```bash
# packages live in the project that owns EF Core — that's MovieData
dotnet add MovieData/MovieData.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.9
dotnet add MovieData/MovieData.csproj package Microsoft.EntityFrameworkCore.Design   --version 10.0.9
dotnet add MovieData/MovieData.csproj package Microsoft.EntityFrameworkCore.Tools    --version 10.0.9
```

**Verify:** `dotnet build` — everything still compiles inside `MovieApi` (nothing moved out yet).
**Commit:** `chore(api): import Övning 3 source into the new solution`

### Step 3: Move entities and DTOs into MovieCore

This is the inner ring. Move `Models/*.cs` and `DTOs/*.cs` from `MovieApi` into `MovieCore`, and rename their namespaces.

```csharp
// MovieCore/Models/Movie.cs   (was MovieApi/Models/Movie.cs)
namespace MovieCore.Models;   // ← was MovieApi.Models

public class Movie
{
    public int Id { get; set; }
    public required string Title { get; set; }
    // ... existing properties unchanged ...
}
```

Update every `using MovieApi.Models;` / `using MovieApi.DTOs;` (in the controllers and context) to `using MovieCore.Models;` / `using MovieCore.DTOs;`.

**Verify:** `dotnet build` — `MovieCore` holds the entities/DTOs; `MovieApi` still compiles via the project reference chain.
**Commit:** `chore(core): move entities and DTOs into MovieCore`

### Step 4: Move the DbContext and seed into MovieData, then re-wire DI

> **New concept: the dependency rule, enforced by the compiler.** `MovieData` is a
> plain class library (`Microsoft.NET.Sdk`) — it has **no** reference to ASP.NET Core.
> The moment you move code into it, anything that reached "outward" to the web host
> stops compiling. That's not a bug to route around; it's the architecture telling you
> the data layer was coupled to the host. Fix the coupling, don't grant the data layer
> access to the web framework.

Do this in four parts; each one fixes the error the previous one exposes. Build after
each part so you see the error count drop.

**4a — Move `MovieContext.cs` into `MovieData` and namespace it.** In Övning 3 it had
no namespace (so it sat in the global namespace and everything saw it for free).

```csharp
// MovieData/MovieContext.cs   (was MovieApi/MovieContext.cs — it had no namespace)
namespace MovieData;

using Microsoft.EntityFrameworkCore;
using MovieCore.Models;

public class MovieContext(DbContextOptions<MovieContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    // ... existing DbSets unchanged ...
}
```

**4b — Move `Extensions/SeedDataExtensions.cs` into `MovieData` and break the host coupling.**
It was an extension on `WebApplication` (lives in `Microsoft.AspNetCore.Builder` — invisible
to a class library). Retarget it to `IServiceProvider`, which the data layer *is* allowed to
know about (the DI abstractions ship transitively with EF Core). Add the explicit
`using` — the web SDK gave it to you implicitly, a class library does not.

```csharp
// MovieData/Extensions/SeedDataExtensions.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;   // ← add: CreateScope / GetRequiredService
using MovieCore.Models;

namespace MovieData.Extensions;

public static class SeedDataExtensions
{
    public static void SeedData(this IServiceProvider services)   // was: this WebApplication app
    {
        using var scope = services.CreateScope();                 // was: app.Services.CreateScope()
        // ... rest of the method unchanged ...
    }
}
```

> ⚠️ Do **not** "fix" this by adding `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
> to `MovieData.csproj`. That makes your inner layer depend on the web framework — the exact
> coupling Clean Architecture exists to prevent.

**4c — Re-wire `Program.cs`.** Hand the host's service provider to the seeder, and add the
usings for the relocated types.

```csharp
// MovieApi/Program.cs   (add the new usings near the top)
using MovieData;
using MovieData.Extensions;   // wherever SeedDataExtensions landed
// ... AddDbContext<MovieContext>(...) is unchanged ...

app.Services.SeedData();   // was: app.SeedData();
```

**4d — Fix the controllers.** `MovieContext` now lives in `MovieData`, so each controller
that injects it (`MoviesController`, `ActorsController`, `ReviewsController`) needs the using.

```csharp
// MovieApi/Controllers/*.cs   (add to the using block of all three)
using MovieData;
```

**Verify:** `dotnet build` → 0 warnings, 0 errors; `dotnet run --project MovieApi` boots;
`GET /api/movies` returns the same data as Övning 3. **The skeleton walks.**
(If your IDE still shows red squiggles after a clean CLI build — even on `WebApplication` —
its project model is stale: reload the solution / invalidate caches. The build is the truth.)
**Commit:** `chore(data): move DbContext and seeding into MovieData`

---

## Phase 1 — MVP

> Goal: deepen the skeleton one vertical slice at a time until every mandatory brief requirement is met. The **Movies** slice is the pilot that drives each new layer into existence; **Reviews** and **Actors** then follow the same shape.

### Step 5: Stub the Movies repository interface

> **New concept: Repository.** An abstraction over data access for one entity type. The *interface* lives in the inner ring (`MovieCore`) and the *implementation* in the outer ring (`MovieData`) — so the compile-time dependency points inward (Dependency Inversion). This is brief **Del 2**.

```csharp
// MovieCore/DomainContracts/IMovieRepository.cs
namespace MovieCore.DomainContracts;
using MovieCore.Models;

public interface IMovieRepository
{
    Task<IEnumerable<Movie>> GetAllAsync();
    Task<Movie?> GetAsync(int id);
    Task<bool> AnyAsync(int id);
    void Add(Movie movie);
    void Update(Movie movie);
    void Remove(Movie movie);
}
```

**Verify:** `dotnet build` — `MovieCore` exposes the interface.
**Commit:** `feat(core): add IMovieRepository contract`

### Step 6: Implement MovieRepository against the context

```csharp
// MovieData/Repositories/MovieRepository.cs
namespace MovieData.Repositories;
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

public class MovieRepository(MovieContext context) : IMovieRepository
{
    public async Task<IEnumerable<Movie>> GetAllAsync() => await context.Movies.ToListAsync();

    public Task<Movie?> GetAsync(int id) => context.Movies.FindAsync(id).AsTask();

    public bool AnyAsync(int id) => context.Movies.Any(m => m.Id == id);  // ← mistake: signature returns Task<bool> and should be awaited async — this is sync and won't compile against the interface

    public void Add(Movie movie) => context.Movies.Add(movie);
    public void Update(Movie movie) => context.Movies.Update(movie);
    public void Remove(Movie movie) => context.Movies.Remove(movie);
}
```

**Verify:** `dotnet build` — once the `AnyAsync` signature matches the interface, it compiles.
**Commit:** `feat(data): implement MovieRepository`

### Step 7: Add the Unit of Work contract and implementation

> **New concept: Unit of Work.** Coordinates several repositories over one `DbContext` and commits them together in a single transaction via `CompleteAsync()` (`SaveChangesAsync`). The repositories share the UoW's context, so one HTTP request = one transaction.

```csharp
// MovieCore/DomainContracts/IUnitOfWork.cs
namespace MovieCore.DomainContracts;

public interface IUnitOfWork
{
    IMovieRepository Movies { get; }
    Task CompleteAsync();
}
```

```csharp
// MovieData/Repositories/UnitOfWork.cs
namespace MovieData.Repositories;
using MovieCore.DomainContracts;

public class UnitOfWork(MovieContext context) : IUnitOfWork
{
    public IMovieRepository Movies { get; } = new MovieRepository(context);
    public Task CompleteAsync() => context.SaveChangesAsync();
}
```

**Verify:** `dotnet build` passes.
**Commit:** `feat(data): add IUnitOfWork and UnitOfWork`

### Step 8: Register the Unit of Work in DI

> **New concept: DI lifetimes.** `Scoped` = one instance per HTTP request, which matches `DbContext`. The brief asks *why `AddScoped`?* — because a longer-lived registration would trap the scoped `DbContext` (a **captive dependency**).

```csharp
// MovieApi/Program.cs   (after AddDbContext)
using MovieCore.DomainContracts;
using MovieData.Repositories;

builder.Services.AddSingleton<IUnitOfWork, UnitOfWork>();  // ← mistake: a singleton captures the scoped DbContext (captive dependency) — should be AddScoped
```

**Verify:** `dotnet run` boots without a lifetime/scope exception on the first request.
**Commit:** `feat(api): register UnitOfWork as scoped`

### Step 9: Re-wire MoviesController to use IUnitOfWork

Swap the controller's direct `MovieContext` use for `IUnitOfWork`. Behaviour stays identical — this is brief **Del 3**.

> **New concept: the strangler refactor.** Rather than a risky big-bang swap of every
> method at once, inject **both** dependencies temporarily — `IUnitOfWork iuw` *and*
> `MovieContext context` — then migrate one endpoint per sub-step. The app compiles and
> runs the *whole* time; you watch each endpoint move and confirm it still behaves before
> touching the next. The old dependency is removed only once nothing uses it (9.7). This
> is the disciplined way to refactor production code, and it's how you build muscle memory:
> small change → `dotnet build` → `dotnet run` → observe → commit.
>
> Two consequences of the **minimal** `IMovieRepository` you'll feel here:
> 1. `GetAsync(id)` is `FindAsync` under the hood — it loads the entity but **no
>    navigations**, and `GetAllAsync()` returns an already-materialised `IEnumerable<Movie>`
>    (no `.Include`). So the old SQL-side projections become in-memory mapping. Correct,
>    slightly less efficient; the projection concern is re-homed to the service layer later.
> 2. `GetMovieDetails` needs `Details`/`Reviews`/`Actors` eager-loaded, which the thin
>    interface can't express — so 9.6 **adds one repository method**. Growing the repo to
>    fit a real query is normal (more methods arrive in Steps 22–24); it isn't a smell.

> **Sub-step naming:** the on-disk controller calls the injected UoW `iuw`; later steps in
> this doc call it `uow`. Same dependency — don't let the rename trip you up.

**9.1 — Repair the constructor (transitional dual dependency).** Delete the broken second
constructor and the `_context` field; take both dependencies on the primary constructor;
rename `_context` → `context` throughout; and fix the `GetMovies` build bug
(`GetAllAsync()` returns `IEnumerable<Movie>`, so the EF-only `ToListAsync()` must become
LINQ-to-Objects `ToList()`, with no `await`).

```csharp
// MovieApi/Controllers/MoviesController.cs
public class MoviesController(IUnitOfWork iuw, MovieContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] string? genre, [FromQuery] int? year, [FromQuery] string? actor)
    {
        var query = await iuw.Movies.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(genre)) query = query.Where(m => m.Genre == genre);
        if (year is not null) query = query.Where(m => m.Year == year);
        if (!string.IsNullOrWhiteSpace(actor)) query = query.Where(m => m.Actors.Any(a => a.Name == actor));

        var movies = query.Select(m => new MovieDto
        {
            Id = m.Id, Title = m.Title, Year = m.Year, Genre = m.Genre, Duration = m.Duration
        }).ToList();
        return Ok(movies);
    }
    // GetMovie/Details/Create/Update/Delete still use `context` for now — migrated in 9.2–9.6.
}
```

> ⚠️ **Known limitation introduced here:** the `?actor=` filter now runs in memory over
> `GetAllAsync()`, which loads **no** `Actors` — so it silently returns nothing. The `genre`
> and `year` filters still work. Restore the actor filter by adding `.Include(m => m.Actors)`
> to the repository's `GetAllAsync()`, or leave it until filtering moves into the service
> layer (Step 22). Flag it; don't let it surprise you.

**Verify:** `dotnet build` green; `dotnet run`; `GET /api/movies` and `?genre=…&year=…` match Övning 3.

**9.2 — Migrate `GetMovie(id)`.** Load the entity through the UoW, then map in memory.

```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<MovieDto>> GetMovie(int id)
{
    var movie = await iuw.Movies.GetAsync(id);
    if (movie is null) return NotFound();
    return Ok(new MovieDto
    {
        Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration
    });
}
```

**Verify:** `dotnet run`; `GET /api/movies/1` → same DTO; `GET /api/movies/9999` → 404.

**9.3 — Migrate `CreateMovie`.** `context.Movies.Add` → `iuw.Movies.Add`; `SaveChangesAsync` → `iuw.CompleteAsync`.

```csharp
[HttpPost]
public async Task<ActionResult<MovieDto>> CreateMovie(MovieCreateDto dto)
{
    var movie = new Movie { Title = dto.Title, Year = dto.Year, Genre = dto.Genre, Duration = dto.Duration };
    iuw.Movies.Add(movie);
    await iuw.CompleteAsync();
    var result = new MovieDto
    {
        Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration
    };
    return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, result);
}
```

**Verify:** `dotnet run`; `POST /api/movies` → 201 + `Location` header; the new id round-trips via `GetMovie`.

**9.4 — Migrate `UpdateMovie`.** `FindAsync` → `GetAsync`; `SaveChangesAsync` → `CompleteAsync`.

```csharp
[HttpPut("{id:int}")]
public async Task<IActionResult> UpdateMovie(int id, MovieUpdateDto dto)
{
    var movie = await iuw.Movies.GetAsync(id);
    if (movie is null) return NotFound();
    movie.Title = dto.Title;
    movie.Year = dto.Year;
    movie.Genre = dto.Genre;
    movie.Duration = dto.Duration;
    await iuw.CompleteAsync();
    return NoContent();
}
```

> **Why no `iuw.Movies.Update(movie)` call?** `GetAsync` (→ `FindAsync`) returns a **tracked**
> entity, so EF's change tracker already sees your property edits; `CompleteAsync` writes
> them. `Update()` is only needed for *detached* entities (e.g. one rebuilt from a DTO).

**Verify:** `dotnet run`; `PUT /api/movies/1` mutates the row; a follow-up `GET` shows the change.

**9.5 — Migrate `DeleteMovie`.** `FindAsync` → `GetAsync`; `Remove`; `CompleteAsync`.

```csharp
[HttpDelete("{id:int}")]
public async Task<IActionResult> DeleteMovie(int id)
{
    var movie = await iuw.Movies.GetAsync(id);
    if (movie is null) return NotFound();
    iuw.Movies.Remove(movie); // cascade delete Reviews and MovieDetails
    await iuw.CompleteAsync();
    return NoContent();
}
```

**Verify:** `dotnet run`; `DELETE /api/movies/{id}` → 204; a second `DELETE` → 404.

**9.6 — Migrate `GetMovieDetails` (grow the repository).** This endpoint needs navigations
the minimal interface can't load, so add one method.

```csharp
// MovieCore/DomainContracts/IMovieRepository.cs   (add)
Task<Movie?> GetWithDetailsAsync(int id);
```

```csharp
// MovieData/Repositories/MovieRepository.cs   (add; needs `using Microsoft.EntityFrameworkCore;`)
public Task<Movie?> GetWithDetailsAsync(int id) =>
    context.Movies
        .Include(m => m.Details)
        .Include(m => m.Reviews)
        .Include(m => m.Actors)
        .FirstOrDefaultAsync(m => m.Id == id);
```

```csharp
// MovieApi/Controllers/MoviesController.cs
[HttpGet("{id:int}/details")]
public async Task<ActionResult<MovieDetailDto>> GetMovieDetails(int id)
{
    var movie = await iuw.Movies.GetWithDetailsAsync(id);
    if (movie is null) return NotFound();

    var dto = new MovieDetailDto
    {
        Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration,
        Synopsis = movie.Details?.Synopsis,
        Language = movie.Details?.Language,
        Budget = movie.Details?.Budget ?? 0,
        Reviews = movie.Reviews.Select(r => new ReviewDto
        {
            Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
        }).ToList(),
        Actors = movie.Actors.Select(a => new ActorDto
        {
            Id = a.Id, Name = a.Name, BirthYear = a.BirthYear
        }).ToList()
    };
    return Ok(dto);
}
```

> **Behaviour nuance:** Övning 3 projected in SQL, so a movie with no `Details` yielded
> `null` columns harmlessly. Loading the entity then writing `movie.Details!.Synopsis`
> would throw a `NullReferenceException` for such a movie — so the mapping above uses `?.`
> and a `?? 0` fallback. Match the null-shape of `MovieDetailDto`'s properties to your DTO.

**Verify:** `dotnet run`; `GET /api/movies/1/details` returns details, reviews, and actors as before.

**9.7 — Drop `MovieContext` from the controller.** Nothing references `context` now, so remove
the constructor parameter and the two usings that only served it (`using MovieData;` and
`using Microsoft.EntityFrameworkCore;` — the controller no longer issues EF calls directly).

```csharp
public class MoviesController(IUnitOfWork iuw) : ControllerBase   // ← MovieContext gone
```

**Verify:** `dotnet build` green; `dotnet run` — every Movies endpoint behaves as before;
`grep -n "MovieContext" MovieApi/Controllers/MoviesController.cs` returns nothing. The
controller now reaches data **only** through `IUnitOfWork`.
**Commit:** `refactor(api): route MoviesController through IUnitOfWork`

### Step 10: Add Review and Actor repositories

Repeat steps 5–7 for the remaining entities, expose them on the UoW, then re-wire
`ReviewsController` and `ActorsController` through `iuw`. (Interfaces go in
`MovieCore.DomainContracts`, implementations in `MovieData.Repositories`.)

> **Sequencing note:** the build stays **red** until the two controller rewrites (10.7–10.8)
> land — the repository/UoW parts alone can't satisfy a controller still calling `context`.
> Add all six parts, then build. (`ReviewsController` may already reference `iuw.Reviews`
> speculatively; that's the gap this step closes.)
>
> **Shape choices:** reviews are always reached *through a movie*, so `IReviewRepository`
> is built around `GetByMovieIdAsync` rather than a generic `GetAll`. Actors are a top-level
> resource with their own endpoints, so `IActorRepository` mirrors the full CRUD shape of
> `IMovieRepository`.

**10.1 — `IReviewRepository`** (new file).

```csharp
// MovieCore/DomainContracts/IReviewRepository.cs
namespace MovieCore.DomainContracts;
using MovieCore.Models;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetByMovieIdAsync(int movieId);
    Task<Review?> GetAsync(int id);
    void Add(Review review);
    void Remove(Review review);
}
```

**10.2 — `ReviewRepository`** (new file).

```csharp
// MovieData/Repositories/ReviewRepository.cs
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class ReviewRepository(MovieContext context) : IReviewRepository
{
    public async Task<IEnumerable<Review>> GetByMovieIdAsync(int movieId) =>
        await context.Reviews.Where(r => r.MovieId == movieId).ToListAsync();

    public Task<Review?> GetAsync(int id) => context.Reviews.FindAsync(id).AsTask();

    public void Add(Review review) => context.Reviews.Add(review);
    public void Remove(Review review) => context.Reviews.Remove(review);
}
```

**10.3 — `IActorRepository`** (new file).

```csharp
// MovieCore/DomainContracts/IActorRepository.cs
namespace MovieCore.DomainContracts;
using MovieCore.Models;

public interface IActorRepository
{
    Task<IEnumerable<Actor>> GetAllAsync();
    Task<Actor?> GetAsync(int id);
    Task<bool> AnyAsync(int id);
    void Add(Actor actor);
    void Update(Actor actor);
    void Remove(Actor actor);
}
```

**10.4 — `ActorRepository`** (new file).

```csharp
// MovieData/Repositories/ActorRepository.cs
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class ActorRepository(MovieContext context) : IActorRepository
{
    public async Task<IEnumerable<Actor>> GetAllAsync() => await context.Actors.ToListAsync();

    public Task<Actor?> GetAsync(int id) => context.Actors.FindAsync(id).AsTask();

    public Task<bool> AnyAsync(int id) => context.Actors.AnyAsync(a => a.Id == id);

    public void Add(Actor actor) => context.Actors.Add(actor);
    public void Update(Actor actor) => context.Actors.Update(actor);
    public void Remove(Actor actor) => context.Actors.Remove(actor);
}
```

**10.5 — Grow the Movie repository.** `AddActorToMovie` needs a movie with its `Actors`
loaded — `GetAsync` (FindAsync) loads no navigations. Add a focused method (same idea as
`GetWithDetailsAsync` from 9.6).

```csharp
// MovieCore/DomainContracts/IMovieRepository.cs   (add)
Task<Movie?> GetWithActorsAsync(int movieId);
```

```csharp
// MovieData/Repositories/MovieRepository.cs   (add; `using Microsoft.EntityFrameworkCore;` already present)
public Task<Movie?> GetWithActorsAsync(int movieId) =>
    context.Movies.Include(m => m.Actors).FirstOrDefaultAsync(m => m.Id == movieId);
```

**10.6 — Expose the repositories on the Unit of Work.**

```csharp
// MovieCore/DomainContracts/IUnitOfWork.cs
namespace MovieCore.DomainContracts;

public interface IUnitOfWork
{
    IMovieRepository Movies { get; }
    IReviewRepository Reviews { get; }
    IActorRepository Actors { get; }
    Task CompleteAsync();
}
```

```csharp
// MovieData/Repositories/UnitOfWork.cs
namespace MovieData.Repositories;
using MovieCore.DomainContracts;

public class UnitOfWork(MovieContext context) : IUnitOfWork
{
    public IMovieRepository Movies { get; } = new MovieRepository(context);
    public IReviewRepository Reviews { get; } = new ReviewRepository(context);
    public IActorRepository Actors { get; } = new ActorRepository(context);
    public Task CompleteAsync() => context.SaveChangesAsync();
}
```

> **No new DI registration needed.** The repositories are constructed *inside* `UnitOfWork`,
> all sharing its one `MovieContext` — one context, one transaction per request. `Program.cs`
> only knows `IUnitOfWork` (registered scoped in Step 8); it never sees the individual
> repositories.

**10.7 — Rewire `ReviewsController`** (drop `MovieContext`; the EF/`MovieData` usings go too).

```csharp
// MovieApi/Controllers/ReviewsController.cs
using Microsoft.AspNetCore.Mvc;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController(IUnitOfWork iuw) : ControllerBase
{
    // GET: api/movies/{movieId}/reviews
    [HttpGet("movies/{movieId}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId)
    {
        if (!await iuw.Movies.AnyAsync(movieId)) return NotFound();

        var reviews = await iuw.Reviews.GetByMovieIdAsync(movieId);
        return Ok(reviews.Select(r => new ReviewDto
        {
            Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
        }).ToList());
    }

    // POST: api/movies/{movieId}/reviews
    [HttpPost("movies/{movieId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(int movieId, ReviewDto dto)
    {
        if (!await iuw.Movies.AnyAsync(movieId)) return NotFound();

        var review = new Review
        {
            MovieId = movieId,
            ReviewerName = dto.ReviewerName,
            Comment = dto.Comment,
            Rating = dto.Rating
        };
        iuw.Reviews.Add(review);
        await iuw.CompleteAsync();
        dto.Id = review.Id;
        return CreatedAtAction(nameof(GetReviews), new { movieId }, dto);
    }

    // DELETE /api/reviews/{id}
    [HttpDelete("reviews/{id:int}")]
    public async Task<ActionResult> DeleteReview(int id)
    {
        var review = await iuw.Reviews.GetAsync(id);
        if (review is null) return NotFound();

        iuw.Reviews.Remove(review);
        await iuw.CompleteAsync();
        return NoContent();
    }
}
```

**10.8 — Rewire `ActorsController`** (straight to `IUnitOfWork`-only — every method maps
cleanly). The N:M add uses `iuw.Movies.GetWithActorsAsync` + `iuw.Actors.GetAsync`.

```csharp
// MovieApi/Controllers/ActorsController.cs
using Microsoft.AspNetCore.Mvc;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class ActorsController(IUnitOfWork iuw) : ControllerBase
{
    // GET: api/actors
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors()
    {
        var actors = await iuw.Actors.GetAllAsync();
        return Ok(actors.Select(a => new ActorDto { Id = a.Id, Name = a.Name, BirthYear = a.BirthYear }).ToList());
    }

    // GET /api/actors/{id}
    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id)
    {
        var actor = await iuw.Actors.GetAsync(id);
        return actor is null
            ? NotFound()
            : Ok(new ActorDto { Id = actor.Id, Name = actor.Name, BirthYear = actor.BirthYear });
    }

    // POST /api/actors
    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var actor = new Actor { Name = dto.Name, BirthYear = dto.BirthYear };
        iuw.Actors.Add(actor);
        await iuw.CompleteAsync();
        dto.Id = actor.Id;   // ← was missing before; the returned body now carries the new id
        return CreatedAtAction(nameof(GetActor), new { id = actor.Id }, dto);
    }

    // PUT /api/actors/{id}
    [HttpPut("actors/{id:int}")]
    public async Task<ActionResult> UpdateActor(int id, ActorDto dto)
    {
        var actor = await iuw.Actors.GetAsync(id);
        if (actor is null) return NotFound();
        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await iuw.CompleteAsync();   // tracked entity → no iuw.Actors.Update(...) needed (cf. Movie 9.4)
        return NoContent();
    }

    // POST /api/movies/{movieId}/actors/{actorId} — add actor to movie, N:M
    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        var movie = await iuw.Movies.GetWithActorsAsync(movieId);
        if (movie is null) return NotFound($"Movie with id {movieId} not found.");

        var actor = await iuw.Actors.GetAsync(actorId);
        if (actor is null) return NotFound($"Actor with id {actorId} not found.");

        if (movie.Actors.Any(a => a.Id == actorId))
            return Conflict($"Actor with id {actorId} is already in movie with id {movieId}.");

        movie.Actors.Add(actor);
        await iuw.CompleteAsync();
        return NoContent();
    }
}
```

> **Two intentional deviations from a byte-for-byte copy:** `CreateActor` now sets `dto.Id`
> before returning (it previously returned `id: 0`), and `GetReviews` checks movie existence
> *before* fetching. Both are behaviour-preserving-or-better.

**Verify:** `dotnet build` green; `dotnet run`; all three controllers work through the UoW;
`POST /api/movies/1/actors/2` → 204, repeat → 409; `grep -rn "MovieContext" MovieApi/Controllers/`
returns nothing (assuming 9.7 is done on `MoviesController`).
**Commit:** `refactor(api): route all controllers through IUnitOfWork`

### Step 11: Add the service-layer projects and wire references

> **New concept: the Clean split.** Controllers should depend on *interfaces*, not on data
> access. We introduce three projects (brief **Del 8**) so the references form the Clean
> shape. `MovieApi` (the composition root) is the one project allowed to reference the
> concrete `MovieData`.

This step is pure scaffolding — no C# to type, just project creation and reference wiring.
The projects are **empty** for now; controllers and services move into them over Steps 12–19.
The payoff is the *reference graph*: once it's right, the compiler physically prevents a
controller from touching `MovieData`.

> **The four projects and why each reference exists:**
> - **MovieContracts** → `MovieCore` only. Holds service interfaces + `IServiceManager`; it
>   speaks in DTOs/entities (Core) and knows nothing about implementations.
> - **MovieServices** → `MovieCore` + `MovieContracts`. Implements the interfaces, so it needs
>   both the contracts it fulfils and the domain types it works with.
> - **MoviePresentation** → `MovieContracts` only. Controllers depend on the `IServiceManager`
>   abstraction — **not** on `MovieServices`, `MovieData`, or `IMapper`. This is the hard rule.
> - **MovieApi** → `MoviePresentation` + `MovieServices` (+ already `MovieData`). The composition
>   root is the *only* place allowed to see every concrete layer, because it's where DI wires
>   interfaces to implementations.

**11.1 — Create the three projects and add them to the solution.** (Commands are identical
in PowerShell; `dotnet` is cross-platform and forward slashes are fine.)

```bash
dotnet new classlib -n MovieContracts    -f net10.0
dotnet new classlib -n MovieServices     -f net10.0
dotnet new classlib -n MoviePresentation -f net10.0   # SDK stays Microsoft.NET.Sdk; Step 12 adds the FrameworkReference
dotnet sln add MovieContracts MovieServices MoviePresentation
```

**11.2 — Delete the default `Class1.cs` stubs.** `dotnet new classlib` drops an empty
`Class1.cs` in each project; remove all three so they don't linger as dead files.

```bash
rm MovieContracts/Class1.cs MovieServices/Class1.cs MoviePresentation/Class1.cs
```

**11.3 — Wire the references (inner rings first).**

```bash
dotnet add MovieContracts/MovieContracts.csproj       reference MovieCore/MovieCore.csproj
dotnet add MovieServices/MovieServices.csproj         reference MovieCore/MovieCore.csproj MovieContracts/MovieContracts.csproj
dotnet add MoviePresentation/MoviePresentation.csproj reference MovieContracts/MovieContracts.csproj
dotnet add MovieApi/MovieApi.csproj                   reference MoviePresentation/MoviePresentation.csproj MovieServices/MovieServices.csproj
```

**11.4 — Verify the dependency graph, not just the build.** A green build only proves it
compiles; the architecture lives in *who references whom*. Confirm `MovieData` is referenced
by exactly one project:

```bash
dotnet build                                          # six projects, 0 errors
grep -rl "MovieData" --include=*.csproj .             # → only MovieApi/MovieApi.csproj
```

> **Sanity check the shape:** `MoviePresentation.csproj` should list **only**
> `MovieContracts` as a project reference. If it somehow references `MovieServices` or
> `MovieData`, the Clean split is already leaking — fix it now, before controllers move in.

**Verify:** `dotnet build` — six projects compile; the only project referencing `MovieData` is `MovieApi`.
**Commit:** `chore(solution): add Contracts/Services/Presentation projects`

### Step 12: Make MoviePresentation able to host controllers

> **New concept: `FrameworkReference` + `AddApplicationPart`.** A controller in a *separate*
> class library needs the ASP.NET framework reference, and the API must be **told** to scan
> that assembly for controllers — otherwise MVC only looks in the entry assembly and the
> library's routes silently 404.

Like Step 11, this is plumbing — you wire the library so it *can* host controllers, but no
controller has moved yet (that's Step 18). Three small parts; each keeps the build green.

**12.1 — Give `MoviePresentation` the ASP.NET framework reference.** A plain class library
(`Microsoft.NET.Sdk`) has no access to `[ApiController]`, `ControllerBase`, `[HttpGet]`,
etc. Add a `FrameworkReference` (not a NuGet `PackageReference`) — it points at the
**shared framework** that's already installed with the runtime, so it adds no package
download and no extra deployed DLLs.

```xml
<!-- MoviePresentation/MoviePresentation.csproj -->
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

> This is the *opposite* of the rule you enforced for `MovieData` in Step 4 (where pulling in
> the web framework would be wrong). The presentation layer **is** web-facing, so depending on
> ASP.NET Core here is correct and expected.

**12.2 — Add an assembly marker.** `AddApplicationPart` needs a *type* from the target
assembly to locate it — but `MoviePresentation` is still empty, so there's nothing to point
at yet. The idiom is a tiny, permanent **marker class** whose only job is to anchor the
assembly. It won't move when controllers come and go, so the wiring in 12.3 never breaks.

```csharp
// MoviePresentation/PresentationAssemblyReference.cs
namespace MoviePresentation;

/// <summary>Marker type used to locate this assembly for MVC application-part scanning.</summary>
public sealed class PresentationAssemblyReference { }
```

**12.3 — Tell the host to scan that assembly for controllers.** `MovieApi` already references
`MoviePresentation` (Step 11.3), so the type is visible. Chain `AddApplicationPart` onto the
existing `AddControllers()`.

```csharp
// MovieApi/Program.cs
builder.Services.AddControllers()
    .AddApplicationPart(typeof(MoviePresentation.PresentationAssemblyReference).Assembly);
```

> **Why a marker instead of `typeof(...Controllers.MoviesController)`?** Pointing at a
> controller would force this line to change every time controllers move, and wouldn't even
> compile right now (no controller lives in `MoviePresentation` until Step 18). The marker
> decouples "find this assembly" from "which controllers it happens to contain."

**Verify:** `dotnet build` passes. Nothing to hit yet — `MoviePresentation` exposes no routes,
and your existing controllers still serve from `MovieApi` via the default entry-assembly scan.
The real proof arrives in Step 18: once `MoviesController` moves into `MoviePresentation`,
its route responds instead of 404 — confirming the application part is wired.
**Commit:** `chore(presentation): enable controller hosting from the library`

### Step 13: Add AutoMapper and a Movie profile

> **New concept: AutoMapper.** Maps entities ↔ DTOs from declarative *profiles* instead of
> the hand-written `new MovieDto { Id = m.Id, ... }` you've been doing in the controllers.
> Profiles live in `MovieData`; services consume the `IMapper` it registers.

**13.1 — Add the package.** It goes in two projects: `MovieData` (because the `Profile`
subclass lives there) and `MovieApi` (because the `AddAutoMapper` DI extension is called
there).

```bash
dotnet add MovieData/MovieData.csproj package AutoMapper --version 16.1.1
dotnet add MovieApi/MovieApi.csproj   package AutoMapper --version 16.1.1   # for the DI registration
```

> **Why pin to `16.1.1`?** Two constraints collide. AutoMapper went **commercial** at v14
> (Lucky Penny Software) — the runtime is free for development/testing but logs a license
> warning and wants a key for production. Meanwhile every version **< 15.1.1** (including the
> last MIT release, `13.0.1`) carries a **high-severity advisory** — CVE-2026-32933, CVSS 7.5:
> uncontrolled recursion on a deeply-nested *circular* graph triggers an uncatchable
> `StackOverflowException` that crashes the whole process (DoS). The fix exists only in
> `15.1.1` / `16.1.1`. So there is **no free *and* patched version** — take the patched one
> (`16.1.1`) and add a free community license key (see 13.3). Verify with
> `dotnet build`: `16.1.1` shows **no** `NU1903` advisory warning; `13.0.1` does.
>
> **Forward note:** `MovieServices` will also need this package the moment `MovieService`
> takes an `IMapper` (Step 15). Add it then — or now — but don't be surprised by the
> "`IMapper` not found" error at Step 15 if you skip it.

**13.2 — Write the Movie profile.** `Movie → MovieDto` is a pure **name match** (`Id`, `Title`,
`Year`, `Genre`, `Duration`), so AutoMapper wires it by convention. `MovieCreateDto → Movie`
is the opposite case: the entity has members the DTO can't supply (`Id` is DB-generated;
`Details`/`Reviews`/`Actors` are navigations). AutoMapper leaves them at defaults *at runtime*,
but `AssertConfigurationIsValid()` will **throw** on those unmapped destination members — so
declare the omission explicitly with `.Ignore()`. That keeps the config validatable and the
intent self-documenting.

```csharp
// MovieData/Mapping/MovieProfile.cs
namespace MovieData.Mapping;
using AutoMapper;
using MovieCore.Models;
using MovieCore.DTOs;

public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<Movie, MovieDto>();

        // Movie's Id is DB-generated and its navigations are populated by EF / later
        // business logic — not from the create DTO. Ignore them so the config is
        // validation-clean (AssertConfigurationIsValid) and the intent is explicit.
        CreateMap<MovieCreateDto, Movie>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore())
            .ForMember(d => d.Reviews, o => o.Ignore())
            .ForMember(d => d.Actors, o => o.Ignore());
    }
}
```

> **Why no `Movie → MovieDetailDto` here?** That DTO *flattens* `Synopsis`/`Language`/`Budget`
> out of the nested `Movie.Details`. AutoMapper's flattening convention looks for source names
> like `DetailsSynopsis`, not `Synopsis`, so a bare `CreateMap` would silently leave those
> three null. It needs explicit `ForMember(d => d.Synopsis, o => o.MapFrom(m => m.Details!.Synopsis))`
> wiring — added when the service actually needs it. For now your Step-9.6 hand-mapping in the
> controller still covers details.

**13.3 — Register AutoMapper in DI, with the license key.** The assembly argument tells it
where to scan for `Profile` subclasses — point it at the `MovieData` assembly via the profile's
own type. Feed the commercial license key from configuration (never hardcode it in source):

```csharp
// MovieApi/Program.cs
builder.Services.AddAutoMapper(
    cfg => cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"],
    typeof(MovieData.Mapping.MovieProfile).Assembly);
```

Get a free community key at <https://luckypennysoftware.com/automapper> and store it in
user-secrets so it never lands in git:

```bash
dotnet user-secrets init --project MovieApi
dotnet user-secrets set "AutoMapper:LicenseKey" "<paste-your-key>" --project MovieApi
```

A missing key is harmless: `Configuration["AutoMapper:LicenseKey"]` returns `null`, AutoMapper
runs and just logs the dev warning until you add it.

> **Optional but recommended:** AutoMapper does **not** validate mappings at startup on its
> own. To catch a broken/incomplete map early instead of at the first request, resolve
> `IMapper` after `build()` in development and call
> `app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();`.
> (This is exactly how the unmapped `MovieCreateDto → Movie` members in 13.2 were caught.)

**Verify:** `dotnet build` → no `NU1903` advisory; `dotnet run` boots and `IMapper` resolves.
(If you added the assertion above, a bad map fails fast at startup with the exact unmapped member.)
**Commit:** `feat(data): add AutoMapper MovieProfile`

### Step 14: Add the exception hierarchy and IExceptionHandler

> **New concept: `IExceptionHandler` + `ProblemDetails`.** Services can't call `NotFound()` —
> that's an MVC concern they have no access to. Instead they **throw** domain exceptions, and
> one centralized handler maps each exception type to a status code + `ProblemDetails` body
> (ADR 0003). This is brief **Del 9**, set up early so services can throw from day one.

**14.1 — The exception hierarchy.** One sealed type per outcome, all under a common abstract
base so the handler can switch on them.

```csharp
// MovieCore/Exceptions/DomainException.cs
namespace MovieCore.Exceptions;

public abstract class DomainException(string message) : Exception(message);
public sealed class NotFoundException(string message) : DomainException(message);
public sealed class BusinessRuleException(string message) : DomainException(message);
```

**14.2 — The handler.** This is a **full class implementing `IExceptionHandler`** — not a
loose method. Type the whole thing, including the class declaration and the `using`s.

```csharp
// MovieApi/ExceptionHandling/DomainExceptionHandler.cs
using Microsoft.AspNetCore.Diagnostics;   // ← where IExceptionHandler lives (NOT an implicit using)
using MovieCore.Exceptions;

namespace MovieApi.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            NotFoundException     => StatusCodes.Status404NotFound,
            BusinessRuleException => StatusCodes.Status400BadRequest,
            _                     => StatusCodes.Status500InternalServerError,
        };
        await Results.Problem(detail: exception.Message, statusCode: status).ExecuteAsync(context);
        return true;
    }
}
```

> ⚠️ **`TryHandleAsync` must sit inside the class — it is not a top-level method.** If you drop
> the method straight into the namespace (no `class ... : IExceptionHandler` around it), the
> compiler emits a confusing cascade that all means the same thing:
> - *"The modifier 'async' is not valid for this item"*
> - *"Top-level statements must precede namespace and type declarations"*
> - *"The 'await' expression can only be used in a method ... marked async"*
> - *"Cannot convert expression type 'bool' to return type 'ValueTask<bool>'"*
> - *"Local function 'TryHandleAsync' is never used"*
>
> The fix for **every** one of those is: wrap the method in the class above. Also note
> `IExceptionHandler` is in `Microsoft.AspNetCore.Diagnostics`, which the Web SDK does **not**
> add implicitly — hence the explicit `using`. (`HttpContext`, `Results`, `StatusCodes` *are*
> implicit, so they need no `using`.)

**14.3 — Wire it into DI and the pipeline.** Two service registrations (before `Build()`) and
one middleware call (after). `AddProblemDetails()` gives the standardized JSON body shape;
`UseExceptionHandler()` (no args) activates the registered `IExceptionHandler`(s). Put the
middleware **early** so it catches exceptions from everything downstream.

```csharp
// MovieApi/Program.cs
using MovieApi.ExceptionHandling;   // for DomainExceptionHandler

// --- with the other builder.Services.* registrations, before builder.Build() ---
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// --- in the pipeline, right after var app = builder.Build(); ---
app.UseExceptionHandler();   // before UseHttpsRedirection / MapControllers
```

**Verify:** `dotnet build` is green. There's nothing throwing domain exceptions yet (services
arrive in Step 15+), so the real proof comes then: a thrown `NotFoundException` surfaces as a
**404 `ProblemDetails` JSON body**, not a 500 stack trace.
**Commit:** `feat(api): map domain exceptions to ProblemDetails`

### Step 15: Stub the Movie service, its contract, and IServiceManager

> **New concept: `IServiceManager` facade.** A single injected entry point exposing each service (lazily). Controllers depend only on `IServiceManager` — never on `DbContext`, `UnitOfWork`, or `IMapper` (brief Del 8, hard requirement).

```csharp
// MovieContracts/IMovieService.cs
namespace MovieContracts;
using MovieCore.DTOs;

public interface IMovieService
{
    Task<MovieDto> GetAsync(int id);
    Task<MovieDto> CreateAsync(MovieCreateDto dto);
}
```

```csharp
// MovieContracts/IServiceManager.cs
namespace MovieContracts;
public interface IServiceManager { IMovieService MovieService { get; } }
```

```csharp
// MovieServices/MovieService.cs   (stub — hardcoded, replaced next step)
public class MovieService(IUnitOfWork uow, IMapper mapper) : IMovieService
{
    public Task<MovieDto> GetAsync(int id) => Task.FromResult(new MovieDto());   // stub
    public Task<MovieDto> CreateAsync(MovieCreateDto dto) => throw new NotImplementedException();
}
```

**Verify:** `dotnet build` passes.
**Commit:** `feat(services): stub MovieService + IServiceManager`

### Step 16: Implement the Movie service

Replace the stub with real logic through the UoW + mapper, throwing `NotFoundException` instead of returning null.

```csharp
// MovieServices/MovieService.cs
public async Task<MovieDto> GetAsync(int id)
{
    var movie = await uow.Movies.GetAsync(id);
    return mapper.Map<MovieDto>(movie);   // ← mistake: movie may be null — throw new NotFoundException($"Movie {id} not found") first
}

public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
{
    var movie = mapper.Map<Movie>(dto);
    uow.Movies.Add(movie);
    await uow.CompleteAsync();
    return mapper.Map<MovieDto>(movie);
}
```

**Verify:** unit-test mentally: a missing id throws `NotFoundException` → 404 via the handler.
**Commit:** `feat(services): implement MovieService get/create`

### Step 17: Implement ServiceManager and register the service layer

```csharp
// MovieServices/ServiceManager.cs
public class ServiceManager(IUnitOfWork uow, IMapper mapper) : IServiceManager
{
    private readonly Lazy<IMovieService> _movie = new(() => new MovieService(uow, mapper));
    public IMovieService MovieService => _movie.Value;
}
```

```csharp
// MovieApi/Program.cs
builder.Services.AddScoped<IServiceManager, ServiceManager>();
```

**Verify:** `dotnet run` boots and resolves `IServiceManager`.
**Commit:** `feat(services): add ServiceManager and register service layer`

### Step 18: Move MoviesController to MoviePresentation and depend on IServiceManager

> **The hard requirement (brief Del 8).** The controller may talk **only** to
> `IServiceManager` → `IMovieService` — never `DbContext`, `IUnitOfWork`, `IMapper`, or a
> repository. `MoviePresentation` doesn't even reference `MovieData`, so a half-migrated
> controller that still reaches for `services.Movies.Add(...)`, `services.CompleteAsync()`,
> or `using MovieData;` **won't compile**. The consequence: *every* operation the controller
> used to do inline (list, details, create, update, delete) must become an **intent-level
> method on `IMovieService`**, with all data access + mapping living in `MovieService`. Moving
> only `GetMovie` (as a first taste) leaves the other five routes with nothing valid to call —
> which is exactly the build break you hit if you only swap the constructor.

**18.1 — Grow `IMovieService` to the full Movies surface.**

```csharp
// MovieContracts/IMovieService.cs
using MovieCore.DTOs;

namespace MovieContracts;

public interface IMovieService
{
    Task<IEnumerable<MovieDto>> GetAllAsync(string? genre, int? year, string? actor);
    Task<MovieDto> GetAsync(int id);
    Task<MovieDetailDto> GetDetailsAsync(int id);
    Task<MovieDto> CreateAsync(MovieCreateDto dto);
    Task UpdateAsync(int id, MovieUpdateDto dto);
    Task DeleteAsync(int id);
}
```

**18.2 — Implement them in `MovieService`.** All the logic that lived in the controller moves
here. "Not found" becomes a thrown `NotFoundException` (the Step-14 handler turns it into a
404 `ProblemDetails`) — the service has no `NotFound()` to call. `GetAsync`/`GetAllAsync`/
`CreateAsync` use the mapper; `GetDetailsAsync` hand-maps the flattened `MovieDetailDto`
(its AutoMapper map was deferred in Step 13 — see the note below).

```csharp
// MovieServices/MovieService.cs
using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class MovieService(IUnitOfWork uow, IMapper mapper) : IMovieService
{
    public async Task<IEnumerable<MovieDto>> GetAllAsync(string? genre, int? year, string? actor)
    {
        var movies = await uow.Movies.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(genre)) movies = movies.Where(m => m.Genre == genre);
        if (year is not null)                  movies = movies.Where(m => m.Year == year);
        if (!string.IsNullOrWhiteSpace(actor)) movies = movies.Where(m => m.Actors.Any(a => a.Name == actor));
        return mapper.Map<IEnumerable<MovieDto>>(movies);
    }

    public async Task<MovieDto> GetAsync(int id)
    {
        var movie = await uow.Movies.GetAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        return mapper.Map<MovieDto>(movie);
    }

    public async Task<MovieDetailDto> GetDetailsAsync(int id)
    {
        var movie = await uow.Movies.GetWithDetailsAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");

        return new MovieDetailDto
        {
            Id = movie.Id, Title = movie.Title, Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration,
            Synopsis = movie.Details?.Synopsis,
            Language = movie.Details?.Language,
            Budget = movie.Details?.Budget ?? 0,
            Reviews = movie.Reviews.Select(r => new ReviewDto
            {
                Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
            }).ToList(),
            Actors = movie.Actors.Select(a => new ActorDto
            {
                Id = a.Id, Name = a.Name, BirthYear = a.BirthYear
            }).ToList()
        };
    }

    public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
    {
        var movie = mapper.Map<Movie>(dto);
        uow.Movies.Add(movie);
        await uow.CompleteAsync();
        return mapper.Map<MovieDto>(movie);
    }

    public async Task UpdateAsync(int id, MovieUpdateDto dto)
    {
        var movie = await uow.Movies.GetAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        movie.Title = dto.Title;
        movie.Year = dto.Year;
        movie.Genre = dto.Genre;
        movie.Duration = dto.Duration;
        await uow.CompleteAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var movie = await uow.Movies.GetAsync(id)
                    ?? throw new NotFoundException($"Movie {id} not found");
        uow.Movies.Remove(movie);
        await uow.CompleteAsync();
    }
}
```

> **Why hand-map the details DTO?** `MovieDetailDto` flattens `Synopsis`/`Language`/`Budget`
> out of `Movie.Details` and projects the `Reviews`/`Actors` collections — the map Step 13
> deliberately left out. You *could* add `CreateMap<Movie, MovieDetailDto>()` (plus
> `Review→ReviewDto`, `Actor→ActorDto`) with `ForMember` flattening to `MovieProfile`; the
> inline version here keeps Step 18 self-contained.
>
> **Carried-over limitation:** the `?actor=` filter still runs over `GetAllAsync()`, which
> loads no `Actors`, so it returns nothing (same as Step 9.1). Paging + proper filtering
> arrive in Step 22.

**18.3 — Make the controller a thin pass-through.** Every action delegates to
`services.MovieService`. Note the shrunken `using` block: **no** `MovieData`,
`Microsoft.EntityFrameworkCore`, `MovieCore.DomainContracts`, or `MovieCore.Models` — the
controller now speaks only `Mvc`, the facade, and DTOs.

```csharp
// MoviePresentation/Controllers/MoviesController.cs
using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController(IServiceManager services) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] string? genre, [FromQuery] int? year, [FromQuery] string? actor)
        => Ok(await services.MovieService.GetAllAsync(genre, year, actor));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDto>> GetMovie(int id)
        => Ok(await services.MovieService.GetAsync(id));

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<MovieDetailDto>> GetMovieDetails(int id)
        => Ok(await services.MovieService.GetDetailsAsync(id));

    [HttpPost]
    public async Task<ActionResult<MovieDto>> CreateMovie(MovieCreateDto dto)
    {
        var created = await services.MovieService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetMovie), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMovie(int id, MovieUpdateDto dto)
    {
        await services.MovieService.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        await services.MovieService.DeleteAsync(id);
        return NoContent();
    }
}
```

> **Behaviour shift (intended):** a missing id on `GET/PUT/DELETE` used to return a bare
> `NotFound()`; it now flows through `NotFoundException` → the handler → a **404 with a
> `ProblemDetails` body**. That's the ADR-0003 house style — consistent errors everywhere.
> `IServiceManager`/`ServiceManager` need **no change**; they already expose `MovieService`.

**Verify:** `dotnet build` is green; `dotnet run` — all six Movies routes behave as before
(missing ids now return 404 `ProblemDetails`);
`grep -rn "DbContext\|UnitOfWork\|IMapper\|MovieData" MoviePresentation/` returns nothing.
**Commit:** `refactor(presentation): MoviesController talks only to IServiceManager`

### Step 19: Repeat the service slice for Reviews and Actors

Exactly the Step-18 shape, applied twice. As there, the one-line "repeat steps 15–18" hides
that your real `ReviewsController` (3 routes) and `ActorsController` (5 routes) each need a
full service surface. After this, **`MovieApi` holds no controllers at all** — every route
lives in `MoviePresentation` and talks only to `IServiceManager`.

> **Mapping choice:** `MovieService` used `IMapper` (a `Movie→MovieDto` map exists). Reviews
> and Actors have **no** profile, so these two services **hand-map** (just like Step 18's
> details) and take **only `IUnitOfWork`** in their constructors — no `IMapper`. That keeps
> Step 19 free of new AutoMapper maps (and away from the `AssertConfigurationIsValid()` check
> your `Program.cs` runs at startup).

**19.1 — `IReviewService` + `ReviewService`.** "Movie missing" / "review missing" become
thrown `NotFoundException` (→ 404 `ProblemDetails`).

```csharp
// MovieContracts/IReviewService.cs
using MovieCore.DTOs;

namespace MovieContracts;

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetByMovieAsync(int movieId);
    Task<ReviewDto> CreateAsync(int movieId, ReviewDto dto);
    Task DeleteAsync(int id);
}
```

```csharp
// MovieServices/ReviewService.cs
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class ReviewService(IUnitOfWork uow) : IReviewService
{
    public async Task<IEnumerable<ReviewDto>> GetByMovieAsync(int movieId)
    {
        if (!await uow.Movies.AnyAsync(movieId))
            throw new NotFoundException($"Movie {movieId} not found");

        var reviews = await uow.Reviews.GetByMovieIdAsync(movieId);
        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id, ReviewerName = r.ReviewerName, Comment = r.Comment, Rating = r.Rating
        }).ToList();
    }

    public async Task<ReviewDto> CreateAsync(int movieId, ReviewDto dto)
    {
        if (!await uow.Movies.AnyAsync(movieId))
            throw new NotFoundException($"Movie {movieId} not found");

        var review = new Review
        {
            MovieId = movieId,
            ReviewerName = dto.ReviewerName,
            Comment = dto.Comment,
            Rating = dto.Rating
        };
        uow.Reviews.Add(review);
        await uow.CompleteAsync();
        dto.Id = review.Id;
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        var review = await uow.Reviews.GetAsync(id)
                     ?? throw new NotFoundException($"Review {id} not found");
        uow.Reviews.Remove(review);
        await uow.CompleteAsync();
    }
}
```

**19.2 — `IActorService` + `ActorService`.** The N:M "add actor to movie" carries the
existence checks and the duplicate guard.

```csharp
// MovieContracts/IActorService.cs
using MovieCore.DTOs;

namespace MovieContracts;

public interface IActorService
{
    Task<IEnumerable<ActorDto>> GetAllAsync();
    Task<ActorDto> GetAsync(int id);
    Task<ActorDto> CreateAsync(ActorDto dto);
    Task UpdateAsync(int id, ActorDto dto);
    Task AddToMovieAsync(int movieId, int actorId);
}
```

```csharp
// MovieServices/ActorService.cs
using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;

namespace MovieServices;

public class ActorService(IUnitOfWork uow) : IActorService
{
    public async Task<IEnumerable<ActorDto>> GetAllAsync()
    {
        var actors = await uow.Actors.GetAllAsync();
        return actors.Select(a => new ActorDto { Id = a.Id, Name = a.Name, BirthYear = a.BirthYear }).ToList();
    }

    public async Task<ActorDto> GetAsync(int id)
    {
        var actor = await uow.Actors.GetAsync(id)
                    ?? throw new NotFoundException($"Actor {id} not found");
        return new ActorDto { Id = actor.Id, Name = actor.Name, BirthYear = actor.BirthYear };
    }

    public async Task<ActorDto> CreateAsync(ActorDto dto)
    {
        var actor = new Actor { Name = dto.Name, BirthYear = dto.BirthYear };
        uow.Actors.Add(actor);
        await uow.CompleteAsync();
        dto.Id = actor.Id;
        return dto;
    }

    public async Task UpdateAsync(int id, ActorDto dto)
    {
        var actor = await uow.Actors.GetAsync(id)
                    ?? throw new NotFoundException($"Actor {id} not found");
        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await uow.CompleteAsync();
    }

    public async Task AddToMovieAsync(int movieId, int actorId)
    {
        var movie = await uow.Movies.GetWithActorsAsync(movieId)
                    ?? throw new NotFoundException($"Movie {movieId} not found");
        var actor = await uow.Actors.GetAsync(actorId)
                    ?? throw new NotFoundException($"Actor {actorId} not found");

        if (movie.Actors.Any(a => a.Id == actorId))
            throw new BusinessRuleException($"Actor {actorId} is already in movie {movieId}");

        movie.Actors.Add(actor);
        await uow.CompleteAsync();
    }
}
```

> ⚠️ **Repo method name:** this calls `uow.Movies.GetWithActorsAsync` (plural). If your repo
> from Step 10 named it `GetWithActorAsync` (singular), make the interface, the implementation,
> and this call all agree — rename to the plural to match this doc. Mismatched names are a
> compile error, not a runtime one, so the build will tell you immediately.

**19.3 — Extend the facade.** Add the two services to `IServiceManager` and construct them
lazily in `ServiceManager`. Note `ReviewService`/`ActorService` get only `uow` (no `mapper`).

```csharp
// MovieContracts/IServiceManager.cs
namespace MovieContracts;

public interface IServiceManager
{
    IMovieService MovieService { get; }
    IReviewService ReviewService { get; }
    IActorService ActorService { get; }
}
```

```csharp
// MovieServices/ServiceManager.cs
using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;

namespace MovieServices;

public class ServiceManager(IUnitOfWork uow, IMapper mapper) : IServiceManager
{
    private readonly Lazy<IMovieService>  _movie  = new(() => new MovieService(uow, mapper));
    private readonly Lazy<IReviewService> _review = new(() => new ReviewService(uow));
    private readonly Lazy<IActorService>  _actor  = new(() => new ActorService(uow));

    public IMovieService  MovieService  => _movie.Value;
    public IReviewService ReviewService => _review.Value;
    public IActorService  ActorService  => _actor.Value;
}
```

**19.4 — Move both controllers into `MoviePresentation`** (thin pass-throughs), then **delete
the originals** from `MovieApi/Controllers/`. Same shrunken `using` block as Step 18.

```csharp
// MoviePresentation/Controllers/ReviewsController.cs
using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController(IServiceManager services) : ControllerBase
{
    [HttpGet("movies/{movieId:int}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId)
        => Ok(await services.ReviewService.GetByMovieAsync(movieId));

    [HttpPost("movies/{movieId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(int movieId, ReviewDto dto)
    {
        var created = await services.ReviewService.CreateAsync(movieId, dto);
        return CreatedAtAction(nameof(GetReviews), new { movieId }, created);
    }

    [HttpDelete("reviews/{id:int}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        await services.ReviewService.DeleteAsync(id);
        return NoContent();
    }
}
```

```csharp
// MoviePresentation/Controllers/ActorsController.cs
using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class ActorsController(IServiceManager services) : ControllerBase
{
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors()
        => Ok(await services.ActorService.GetAllAsync());

    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id)
        => Ok(await services.ActorService.GetAsync(id));

    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var created = await services.ActorService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetActor), new { id = created.Id }, created);
    }

    [HttpPut("actors/{id:int}")]
    public async Task<IActionResult> UpdateActor(int id, ActorDto dto)
    {
        await services.ActorService.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        await services.ActorService.AddToMovieAsync(movieId, actorId);
        return NoContent();
    }
}
```

```bash
# delete the old copies so MovieApi hosts no controllers and routes don't collide
rm MovieApi/Controllers/ReviewsController.cs MovieApi/Controllers/ActorsController.cs
```

> **Behaviour shifts (intended, both ADR-0003 house style):**
> - Missing ids on reviews/actors now return **404 `ProblemDetails`** (via `NotFoundException`),
>   not a bare `NotFound()`.
> - "Actor already in movie" now returns **400 `BusinessRuleException`** instead of the old
>   `409 Conflict` — this aligns with how Step 23 treats the duplicate-actor rule. (If you'd
>   rather keep a true 409, add a `ConflictException` to the hierarchy and map it in the handler.)

**Verify:** `dotnet build` is green; `dotnet run` — all Movies/Reviews/Actors routes work
end-to-end through the service layer; `MovieApi/Controllers/` is empty; `POST /api/movies/1/actors/2`
→ 204, repeat → 400 `ProblemDetails`;
`grep -rn "DbContext\|UnitOfWork\|IMapper\|MovieData" MoviePresentation/` returns nothing.
**Commit:** `refactor(presentation): move Review and Actor controllers behind IServiceManager`

### Step 20: Normalise Genre into a many-to-many relationship

> **New concept: M:N via a join.** Replace the `Genre` string on `Movie` with a `Genre` entity and a collection (ADR 0002). EF builds the join table implicitly.

```csharp
// MovieCore/Models/Genre.cs
namespace MovieCore.Models;
public class Genre
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
```

```csharp
// MovieCore/Models/Movie.cs   (replace the string Genre)
public ICollection<Genre> Genres { get; set; } = new List<Genre>();   // was: public required string Genre

// MovieCore/Models/Genres.cs  — one source of truth for the well-known name
public static class Genres { public const string Documentary = "Documentary"; }
```

> **The string is gone — fix every reference before you build.** Removing the `Genre`
> string breaks four call sites plus the AutoMapper map. The read DTOs keep their single
> `Genre` string, now surfaced as a comma-joined display of the genre names (least churn;
> the API contract shape is unchanged). The create side moves to `GenreIds` in Step 21.

```csharp
// MovieServices/MovieService.cs

// GetAllAsync — filter on the collection, not a string.
// Note: GetAllAsync materialises with ToListAsync(), so this Where runs in memory (LINQ-to-Objects).
// C# string == is ordinal & case-SENSITIVE, so compare with OrdinalIgnoreCase for a forgiving filter
// (?genre=drama would otherwise miss "Drama"). Same applies to the actor filter below.
if (!string.IsNullOrWhiteSpace(genre))
    movies = movies.Where(m => m.Genres.Any(g => string.Equals(g.Name, genre, StringComparison.OrdinalIgnoreCase)));

// GetDetailsAsync — join the names for the display DTO
Genre = string.Join(", ", movie.Genres.Select(g => g.Name)),

// UpdateAsync — DELETE `movie.Genre = dto.Genre;`
// (genres are a collection now; they're set on create via GenreIds in Step 21)
```

```csharp
// MovieData/Mapping/MovieProfile.cs
//   MovieDto.Genre lost its matching source, and Movie.Genres is a new unmapped target —
//   state both explicitly or AssertConfigurationIsValid() throws at startup.
CreateMap<Movie, MovieDto>()
    .ForMember(d => d.Genre, o => o.MapFrom(s => string.Join(", ", s.Genres.Select(g => g.Name))));

CreateMap<MovieCreateDto, Movie>()
    .ForMember(d => d.Id, o => o.Ignore())
    .ForMember(d => d.Details, o => o.Ignore())
    .ForMember(d => d.Reviews, o => o.Ignore())
    .ForMember(d => d.Actors, o => o.Ignore())
    .ForMember(d => d.Genres, o => o.Ignore());   // ← new
```

```csharp
// MovieData/Extensions/SeedDataExtensions.cs — seed a Genre entity, attach it to the movie
var drama = new Genre { Name = "Drama" };
// ...
var movie = new Movie
{
    Title = "Forrest Gump",
    Year = 1994,
    Genres = { drama },        // ← was: Genre = "Drama"
    Duration = 142,
    // ...
};
```

```csharp
// MovieData/Repositories/MovieRepository.cs — eager-load what the filters touch, or they match nothing.
// The list endpoint filters on BOTH m.Genres (genre) and m.Actors (actor), so include both —
// otherwise the unloaded collection is empty and that filter silently returns [].
public async Task<IEnumerable<Movie>> GetAllAsync() =>
    await context.Movies.Include(m => m.Genres).Include(m => m.Actors).ToListAsync();

// FindAsync can't Include — switch to a query so the single-movie endpoint shows genres too
public Task<Movie?> GetAsync(int id) =>
    context.Movies.Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id);

public Task<Movie?> GetWithDetailsAsync(int id) => context.Movies
    .Include(m => m.Details)
    .Include(m => m.Reviews)
    .Include(m => m.Actors)
    .Include(m => m.Genres)        // ← new
    .FirstOrDefaultAsync(m => m.Id == id);
```

```bash
# only after the solution builds — EF can't add a migration to a project that won't compile
dotnet ef migrations add GenreManyToMany --project MovieData --startup-project MovieApi
dotnet ef database update --project MovieData --startup-project MovieApi
```

**Verify:** `dotnet build` is green; migration applies; a movies↔genres join table exists;
seed creates a `Drama` genre linked to Forrest Gump; `GET /api/movies?genre=Drama` returns it
and its `genre` field reads `Drama`.
**Commit:** `feat(core): model Genre as many-to-many`

### Step 21: Require valid genres when creating a movie

A create must reference **≥1** genre and **all** must exist, else `BusinessRuleException` → 400
`ProblemDetails` (ADR 0002). Step 20 made the write routes genre-blind (the create map ignores
`Genres`); this step gives the client a way to pass genres — a list of **ids** — and validates them
in the service. Do the four sub-steps in order; each one is needed before the project builds again.

**21.1 — Swap the `Genre` string for `GenreIds` on the create DTO.**
The old `string Genre` is dead now (Step 20 stopped mapping it). Replace it with the id list the
service will look up. Leave `MovieUpdateDto` alone for now (see the note at the end of the step).

```csharp
// MovieCore/DTOs/MovieCreateDto.cs   (inside MovieCreateDto — replace the string Genre property)
public List<int> GenreIds { get; set; } = [];   // was: [Required] public string Genre
```

> No `[Required]`/`[MinLength]` here on purpose: we want an empty list to reach the **service** and
> throw a `BusinessRuleException` (ADR-0002, a real 400 `ProblemDetails`), not be short-circuited by
> model-state validation. Letting the service own the rule is the whole point of the step.

**21.2 — Add a Genre repository and expose it on the Unit of Work.**
`Genre` is in the EF model (via `Movie.Genres`) but has no repository yet. Four small files, mirroring
the existing repos:

```csharp
// MovieCore/DomainContracts/IGenreRepository.cs
using MovieCore.Models;
namespace MovieCore.DomainContracts;

public interface IGenreRepository
{
    Task<List<Genre>> GetByIdsAsync(IEnumerable<int> ids);
}
```

```csharp
// MovieData/Repositories/GenreRepository.cs
using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;
namespace MovieData.Repositories;

public class GenreRepository(MovieContext context) : IGenreRepository
{
    public async Task<List<Genre>> GetByIdsAsync(IEnumerable<int> ids) =>
        await context.Genres.Where(g => ids.Contains(g.Id)).ToListAsync();
}
```

```csharp
// MovieData/MovieContext.cs   (add the DbSet so context.Genres exists)
public DbSet<Genre> Genres => Set<Genre>();
```

```csharp
// MovieCore/DomainContracts/IUnitOfWork.cs   (add the property)
IGenreRepository Genres { get; }
```

```csharp
// MovieData/Repositories/UnitOfWork.cs   (construct it like the others)
public IGenreRepository Genres { get; } = new GenreRepository(context);
```

> ⚠️ **Adding `DbSet<Genre> Genres` renames a table.** Step 20's migration created the join's
> principal table as `Genre` (named after the entity type). Once a `DbSet` named `Genres` exists,
> EF's convention names the table after the property — `Genres` — which is a real model change. If
> you skip the migration you'll hit `PendingModelChangesWarning` at startup. Capture it:
>
> ```bash
> dotnet ef migrations add AddGenresDbSet --project MovieData --startup-project MovieApi
> ```
>
> (The migration just renames `Genre` → `Genres`; on a dropped DB it's created with the right name.)

**21.3 — Validate and assign the genres in `CreateAsync`.**
The doc snippet earlier set `movie.Genres` on a variable that didn't exist yet — here is the whole
method so the order is unambiguous (validate → map → assign → save):

```csharp
// MovieServices/MovieService.cs   (replace CreateAsync)
public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
{
    if (dto.GenreIds is null || dto.GenreIds.Count == 0)
        throw new BusinessRuleException("A movie needs at least one genre.");

    var genres = await uow.Genres.GetByIdsAsync(dto.GenreIds);
    if (genres.Count != dto.GenreIds.Distinct().Count())
        throw new BusinessRuleException("One or more genres do not exist.");

    var movie = mapper.Map<Movie>(dto);
    movie.Genres = genres;            // the create map ignores Genres, so set them explicitly
    uow.Movies.Add(movie);
    await uow.CompleteAsync();
    return mapper.Map<MovieDto>(movie);   // response shows "genre": joined names (Step 20 map)
}
```

**21.4 — Seed a couple of standalone genres so POSTs have real ids to reference.**
Without this you'd only have `Drama` (id 1, from the movie graph) and nothing to demonstrate a
multi-genre create. Add the extra genres before the movie:

```csharp
// MovieData/Extensions/SeedDataExtensions.cs   (after `var drama = new Genre { Name = "Drama" };`)
var comedy = new Genre { Name = "Comedy" };
context.Genres.AddRange(drama, comedy);   // fresh DB → Drama = id 1, Comedy = id 2
// (the movie still links Drama via `Genres = { drama }` — same instance, inserted once)
```

Genres are DB-assigned in insert order, so on a freshly dropped database Drama is `1`, Comedy is `2`.
Because you changed the seed, drop and re-run so it takes effect:

```bash
dotnet ef database drop -f --project MovieData --startup-project MovieApi
dotnet run --project MovieApi
```

**Verify (expected responses):**

| Request body to `POST /api/movies` | Result |
|---|---|
| `{ "title": "X", "year": 2000, "duration": 100 }` (no `genreIds`) | **400** `ProblemDetails` — "A movie needs at least one genre." |
| `{ ..., "genreIds": [] }` | **400** — same rule |
| `{ ..., "genreIds": [999] }` (unknown id) | **400** — "One or more genres do not exist." |
| `{ ..., "genreIds": [1, 2] }` | **201 Created**, `Location` header set, body `"genre": "Drama, Comedy"` |

**Commit:** `feat(services): validate genres on movie create`

> **Aside — `MovieUpdateDto.Genre` is now vestigial.** `PUT` still requires a `genre` string in the
> payload (it's `[Required]`) but `UpdateAsync` ignores it. Updating a movie's genres is a separate
> concern not covered here; if the leftover `[Required] string Genre` on `MovieUpdateDto` bothers you,
> deleting that property is safe (nothing reads it) and stops `PUT` from demanding a genre. Leaving it
> is also fine — your call.

### Step 21b: Make the remaining list filters case-insensitive

Step 20 fixed the **genre** filter to compare with `OrdinalIgnoreCase` (the `Where` runs in memory, so
C#'s case-sensitive `==` would miss `?genre=drama`). The **actor** filter on the next line has the
exact same bug — it's the only other in-memory string-equality filter in the codebase. Bring it in line:

```csharp
// MovieServices/MovieService.cs   (GetAllAsync — actor filter)
if (!string.IsNullOrWhiteSpace(actor))
    movies = movies.Where(m => m.Actors.Any(a => string.Equals(a.Name, actor, StringComparison.OrdinalIgnoreCase)));
```

**Verify:** `GET /api/movies?actor=tom hanks` returns Forrest Gump (lower-case now matches `"Tom Hanks"`),
just as `?genre=drama` already does.
**Commit:** `fix(services): make actor filter case-insensitive`

### Step 22: Add paging with an X-Pagination header

**22.1 — Enrich the seed data (recommended before you test paging).**
One movie can't demonstrate paging, filters, or the rules in Steps 24–25. Replace the seed body
(everything after the `if (context.Movies.Any()) return;` guard) with a small but varied dataset:
multiple `Drama` movies, actors who appear in more than one film, a year spread, **uneven review
counts** (one movie with zero), and one **Documentary** — exactly the shapes the next steps need.

```csharp
// MovieData/Extensions/SeedDataExtensions.cs   (replace the body after the idempotency guard)

// --- Genres (ids 1..4 on a fresh DB) ---
var drama       = new Genre { Name = "Drama" };
var comedy      = new Genre { Name = "Comedy" };
var documentary = new Genre { Name = Genres.Documentary };   // single source of truth (Step 20)
var sciFi       = new Genre { Name = "Sci-Fi" };
context.Genres.AddRange(drama, comedy, documentary, sciFi);

// --- Actors (ids 1..14) ---
var hanks     = new Actor { Name = "Tom Hanks",          BirthYear = 1956 };
var robbins   = new Actor { Name = "Tim Robbins",        BirthYear = 1958 };
var freeman   = new Actor { Name = "Morgan Freeman",     BirthYear = 1937 };
var johansson = new Actor { Name = "Scarlett Johansson", BirthYear = 1984 };
var murray    = new Actor { Name = "Bill Murray",        BirthYear = 1950 };
// extra cast (ids 6..14) so the Documentary can hold 10 actors and the cap is testable (Step 25)
var attenborough = new Actor { Name = "David Attenborough", BirthYear = 1926 };
var herzog       = new Actor { Name = "Werner Herzog",      BirthYear = 1942 };
var weaver       = new Actor { Name = "Sigourney Weaver",   BirthYear = 1949 };
var jones        = new Actor { Name = "James Earl Jones",   BirthYear = 1931 };
var irons        = new Actor { Name = "Jeremy Irons",       BirthYear = 1948 };
var mirren       = new Actor { Name = "Helen Mirren",       BirthYear = 1945 };
var neeson       = new Actor { Name = "Liam Neeson",        BirthYear = 1952 };
var blanchett    = new Actor { Name = "Cate Blanchett",     BirthYear = 1969 };
var elba         = new Actor { Name = "Idris Elba",         BirthYear = 1972 };
context.Actors.AddRange(hanks, robbins, freeman, johansson, murray,
    attenborough, herzog, weaver, jones, irons, mirren, neeson, blanchett, elba);

// --- Movies (ids 1..6) — varied genres, shared actors, uneven review counts ---
var movies = new List<Movie>
{
    new()
    {
        Title = "Forrest Gump", Year = 1994, Duration = 142,
        Genres = { drama },
        Actors = { hanks },
        Details = new MovieDetails { Synopsis = "Life is like a box of chocolates", Language = "English", Budget = 55_000_000m },
        Reviews =
        {
            new Review { ReviewerName = "Alice", Comment = "Classic!", Rating = 5 },
            new Review { ReviewerName = "Bob",   Comment = "Touching.", Rating = 4 }
        }
    },
    new()
    {
        Title = "The Shawshank Redemption", Year = 1994, Duration = 142,
        Genres = { drama },
        Actors = { robbins, freeman },
        // 8 staggered reviews — old movie (1994) over the 5-cap → the review-trimmer's target (Steps 29/30)
        Reviews =
        {
            new Review { ReviewerName = "Cara",   Comment = "Masterpiece.",   Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-8) },
            new Review { ReviewerName = "Dan",    Comment = "Hopeful.",       Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new Review { ReviewerName = "Eve",    Comment = "Slow start.",    Rating = 3, CreatedAt = DateTime.UtcNow.AddDays(-6) },
            new Review { ReviewerName = "Greta",  Comment = "Unforgettable.", Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new Review { ReviewerName = "Hans",   Comment = "A classic.",     Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new Review { ReviewerName = "Ingrid", Comment = "Powerful.",      Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Review { ReviewerName = "Jonas",  Comment = "Moving.",        Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Review { ReviewerName = "Karin",  Comment = "Brilliant.",     Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        }
    },
    new()
    {
        Title = "Lost in Translation", Year = 2003, Duration = 102,
        Genres = { drama, comedy },
        Actors = { johansson, murray },
        Reviews = { new Review { ReviewerName = "Finn", Comment = "Quietly great.", Rating = 4 } }
    },
    new()
    {
        Title = "Groundhog Day", Year = 1993, Duration = 101,
        Genres = { comedy },
        Actors = { murray }
        // no reviews — exercises the zero-review case (Step 24)
    },
    new()
    {
        Title = "March of the Penguins", Year = 2005, Duration = 80,
        Genres = { documentary },
        // 10 actors — at the Documentary cap, so an 11th POST → 400 (Step 25)
        Actors = { freeman, attenborough, herzog, weaver, jones, irons, mirren, neeson, blanchett, elba },
        Reviews = { new Review { ReviewerName = "Gil", Comment = "Beautiful.", Rating = 4 } }
    },
    new()
    {
        Title = "Her", Year = 2013, Duration = 126,
        Genres = { drama, sciFi },
        Actors = { johansson },
        // 9 reviews — recent movie (2013), so the 10-cap applies; one more POST hits 10, the next 400s (Step 24)
        Reviews =
        {
            new Review { ReviewerName = "Hana", Comment = "Melancholic.",          Rating = 5 },
            new Review { ReviewerName = "Ivan", Comment = "Thought-provoking.",     Rating = 4 },
            new Review { ReviewerName = "Judy", Comment = "Beautifully shot.",      Rating = 5 },
            new Review { ReviewerName = "Kyle", Comment = "Unsettling and tender.", Rating = 4 },
            new Review { ReviewerName = "Lena", Comment = "Loved the score.",       Rating = 5 },
            new Review { ReviewerName = "Milo", Comment = "A touch slow.",          Rating = 3 },
            new Review { ReviewerName = "Nora", Comment = "The future feels real.", Rating = 4 },
            new Review { ReviewerName = "Omar", Comment = "Heartbreaking.",         Rating = 5 },
            new Review { ReviewerName = "Pia",  Comment = "Bittersweet.",           Rating = 4 }
        }
    }
};

context.Movies.AddRange(movies);
context.SaveChanges();
```

Ids are DB-assigned in insert order, so on a **freshly dropped** database they land predictably —
and `Forrest Gump`/`Drama` stay at id `1`, so the earlier verify steps still hold:

| Entity | Ids (fresh DB) |
|---|---|
| Genres | 1 Drama · 2 Comedy · 3 Documentary · 4 Sci-Fi |
| Actors | 1 Tom Hanks · 2 Tim Robbins · 3 Morgan Freeman · 4 Scarlett Johansson · 5 Bill Murray · 6–14 extra cast (Attenborough…Elba, on the Documentary) |
| Movies | 1 Forrest Gump · 2 Shawshank · 3 Lost in Translation · 4 Groundhog Day · 5 March of the Penguins · 6 Her |

Drop and re-run so the new seed takes effect (the guard skips seeding if any movie already exists):

```bash
dotnet ef database drop -f --project MovieData --startup-project MovieApi
dotnet run --project MovieApi
```

**Verify the dataset (sanity checks for the filters you already built):**

| Request | Returns |
|---|---|
| `GET /api/movies?genre=drama` | Forrest Gump, Shawshank, Lost in Translation, Her (4) |
| `GET /api/movies?genre=comedy` | Lost in Translation, Groundhog Day (2) |
| `GET /api/movies?actor=bill murray` | Lost in Translation, Groundhog Day (2) |
| `GET /api/movies?year=1994` | Forrest Gump, Shawshank (2) |

**Commit:** `chore(data): seed a richer movie dataset`

---

**22.2 — Add paging — alongside the filters, not instead of them.**

> **Paging is another facet of the same query.** `GET /api/movies` already binds `genre`/`year`/`actor`.
> Paging doesn't get its own endpoint and it must **not** replace those parameters — the filters narrow
> the set, then `page`/`pageSize` slice what's left. So `GetMovies` gains paging *next to* the filters.

> **New concept: complex query binding.** Simple params (`string? genre`) bind from the query string by
> convention. A complex object like `PaginationParameters` needs **`[FromQuery]`** so the binder reads
> its properties (`page`, `pageSize`) from the query string instead of expecting them in the body.

```csharp
// MovieCore/DTOs/PaginationParameters.cs
namespace MovieCore.DTOs;
public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    public int Page { get; set; } = 1;
    public int PageSize { get => _pageSize; set => _pageSize = value > MaxPageSize ? MaxPageSize : value; }
}
```

```csharp
// MovieCore/DTOs/PagedResult.cs — the slice plus the metadata that goes in the header
namespace MovieCore.DTOs;

public record PaginationMeta(int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record PagedResult<T>(IReadOnlyList<T> Data, PaginationMeta Meta);
```

```csharp
// MovieContracts/IMovieService.cs — add next to GetAllAsync (same filter params + paging)
Task<PagedResult<MovieDto>> GetPageAsync(string? genre, int? year, string? actor, PaginationParameters paging);
```

The filter rules now live in one private helper so `GetAllAsync` and `GetPageAsync` can't drift apart:

```csharp
// MovieServices/MovieService.cs

// filters in one place — note OrdinalIgnoreCase on genre & actor (Steps 20 / 21b)
private static IEnumerable<Movie> ApplyFilters(IEnumerable<Movie> movies, string? genre, int? year, string? actor)
{
    if (!string.IsNullOrWhiteSpace(genre)) movies = movies.Where(m => m.Genres.Any(g => string.Equals(g.Name, genre, StringComparison.OrdinalIgnoreCase)));
    if (year is not null)                  movies = movies.Where(m => m.Year == year);
    if (!string.IsNullOrWhiteSpace(actor)) movies = movies.Where(m => m.Actors.Any(a => string.Equals(a.Name, actor, StringComparison.OrdinalIgnoreCase)));
    return movies;
}

// GetAllAsync now just maps the filtered set (re-using the helper)
public async Task<IEnumerable<MovieDto>> GetAllAsync(string? genre, int? year, string? actor) =>
    mapper.Map<IEnumerable<MovieDto>>(ApplyFilters(await uow.Movies.GetAllAsync(), genre, year, actor));

// GetPageAsync filters, counts the FULL filtered set, then slices
public async Task<PagedResult<MovieDto>> GetPageAsync(string? genre, int? year, string? actor, PaginationParameters paging)
{
    var movies = ApplyFilters(await uow.Movies.GetAllAsync(), genre, year, actor);

    var total = movies.Count();   // count before Skip/Take so TotalPages is correct
    var items = movies
        .Skip((paging.Page - 1) * paging.PageSize)
        .Take(paging.PageSize);

    return new PagedResult<MovieDto>(
        mapper.Map<List<MovieDto>>(items),
        new PaginationMeta(paging.Page, paging.PageSize, total));
}
```

```csharp
// MoviePresentation/Controllers/MoviesController.cs
using System.Text.Json;   // add at the top, for JsonSerializer
// ...
[HttpGet]
public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
    [FromQuery] string? genre,
    [FromQuery] int? year,
    [FromQuery] string? actor,
    [FromQuery] PaginationParameters paging)        // ← [FromQuery] is what makes page/pageSize bind
{
    var page = await services.MovieService.GetPageAsync(genre, year, actor, paging);
    Response.Headers["X-Pagination"] = JsonSerializer.Serialize(page.Meta);
    return Ok(page.Data);
}
```

> **Where the slicing happens.** This pages **in memory** — `GetAllAsync()` already does `ToListAsync()`,
> so `Skip/Take` runs on objects, matching the existing filter approach. Fine for this dataset; in a real
> app you'd push paging into the query (`IQueryable` `Skip/Take` + `CountAsync`) so the DB returns only
> one page. That's a repository refactor you can do later — it doesn't change this controller or contract.

**Verify (filters + paging combine):**

| Request | `Data` | `X-Pagination` header |
|---|---|---|
| `GET /api/movies?pageSize=2&page=2` | movies 3–4 (Lost in Translation, Groundhog Day) | `TotalCount` 6, `TotalPages` 3 |
| `GET /api/movies?genre=drama&pageSize=2&page=1` | first 2 of the 4 Drama films | `TotalCount` 4, `TotalPages` 2 |
| `GET /api/movies?pageSize=110` | all 6 (clamped to ≤100/page) | `PageSize` 100 |

**Commit:** `feat(services): page list endpoints with X-Pagination header`

### Step 23: Enforce the unique-title rule in the service

> **Scope check first.** Del 6 lists three structural rules, but only one is actually place-able here:
> - **Duplicate title** (rule 2) — new; wired below, in the service, as a `BusinessRuleException` → 400.
> - **Actor can't be added twice** (rule 6) — **already done** in `ActorService.AddToMovieAsync`
>   (Step 19's `if (movie.Actors.Any(a => a.Id == actorId)) throw new BusinessRuleException(...)`).
>   Nothing to add — re-read that guard and confirm it's there.
> - **Negative budget** (rule 3) — `Budget` lives on `MovieDetails`, which has **no write path** until
>   **Step 26** (PATCH). No create/update DTO carries a budget today, so the guard has nowhere to live
>   yet; it moves to Step 26. (The original snippet's `dto.Budget` wouldn't even compile.)

**23.1 — Add a `TitleExistsAsync` lookup to the Movie repository.**
One method serves both create and update: the optional `excludeId` lets update ignore the movie's
**own** current title — otherwise saving a movie without renaming it would falsely trip the rule.

```csharp
// MovieCore/DomainContracts/IMovieRepository.cs   (add)
Task<bool> TitleExistsAsync(string title, int? excludeId = null);
```

```csharp
// MovieData/Repositories/MovieRepository.cs   (add)
public Task<bool> TitleExistsAsync(string title, int? excludeId = null) =>
    context.Movies.AnyAsync(m => m.Title == title && (excludeId == null || m.Id != excludeId));
```

> This runs in SQL (`AnyAsync` is translated), so it's case-insensitive under the default collation —
> `"forrest gump"` collides with `"Forrest Gump"`, which matches the intent of "no duplicate titles".

**23.2 — Reject a duplicate title on create.**
Guard at the top of `CreateAsync` (full method shown so placement is unambiguous):

```csharp
// MovieServices/MovieService.cs
public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
{
    if (await uow.Movies.TitleExistsAsync(dto.Title))
        throw new BusinessRuleException($"A movie titled '{dto.Title}' already exists.");

    if (dto.GenreIds is null || dto.GenreIds.Count == 0)
        throw new BusinessRuleException("A movie needs at least one genre.");

    var genres = await uow.Genres.GetByIdsAsync(dto.GenreIds);
    if (genres.Count != dto.GenreIds.Distinct().Count())
        throw new BusinessRuleException("One or more genres do not exist.");

    var movie = mapper.Map<Movie>(dto);
    movie.Genres = genres;
    uow.Movies.Add(movie);
    await uow.CompleteAsync();
    return mapper.Map<MovieDto>(movie);
}
```

**23.3 — Reject a colliding rename, but allow a no-op rename — and drop the dead `Genre` field.**
`MovieUpdateDto` still carries `[Required] string Genre`, but `UpdateAsync` stopped reading it back in
Step 20. Leaving it forces every `PUT` to send a meaningless genre. Delete it, then add the guard with
`id` passed in so the movie can't collide with itself:

```csharp
// MovieCore/DTOs/MovieCreateDto.cs   (inside MovieUpdateDto — DELETE this property)
//   [Required] public string Genre { get; set; } = string.Empty;   ← unused since Step 20
```

```csharp
// MovieServices/MovieService.cs
public async Task UpdateAsync(int id, MovieUpdateDto dto)
{
    var movie = await uow.Movies.GetAsync(id)
                ?? throw new NotFoundException($"Movie {id} not found");

    if (await uow.Movies.TitleExistsAsync(dto.Title, id))   // excludeId = id → ignore itself
        throw new BusinessRuleException($"A movie titled '{dto.Title}' already exists.");

    movie.Title = dto.Title;
    movie.Year = dto.Year;
    movie.Duration = dto.Duration;
    await uow.CompleteAsync();
}
```

**Verify (run against the Step 22.1 seed):**

| Request | Result |
|---|---|
| `POST /api/movies` with `"title": "Forrest Gump"` (seeded) | **400** — "A movie titled 'Forrest Gump' already exists." |
| `POST /api/movies` with a fresh title + valid `genreIds` | **201 Created** |
| `PUT /api/movies/1` keeping `"title": "Forrest Gump"` | **204** — self-exclusion, no false collision |
| `PUT /api/movies/1` with `"title": "Her"` (movie 6's title) | **400** — duplicate |
| `POST /api/movies/1/actors/1` twice | **400** on the repeat — already enforced (Step 19) |

**Commit:** `feat(services): enforce unique movie title`

### Step 24: Enforce the review-count rules

> Del 6 rules 1 & 4 (sync half): a movie may have at most **10** reviews; if it's **older than 20 years**,
> at most **5**. Both live in the service as `BusinessRuleException` → 400. They go in
> `ReviewService.CreateAsync` (there is **no** `AddReviewAsync`), and they need the movie's reviews loaded.

**24.1 — Add a `GetWithReviewsAsync` lookup to the Movie repository.**
The cap needs `movie.Reviews` (and `Year`) loaded; the current existence check (`AnyAsync`) only returns a
bool, and without an `Include` the collection is empty so the rule never fires. Add it (mirrors
`GetWithActorsAsync`):

```csharp
// MovieCore/DomainContracts/IMovieRepository.cs   (add)
Task<Movie?> GetWithReviewsAsync(int id);
```

```csharp
// MovieData/Repositories/MovieRepository.cs   (add)
public Task<Movie?> GetWithReviewsAsync(int id) =>
    context.Movies.Include(m => m.Reviews).FirstOrDefaultAsync(m => m.Id == id);
```

**24.2 — Enforce both caps in `CreateAsync`.**
Swap the `AnyAsync` existence check for the loaded movie, then guard before building the review. Full
method (note **`>= 10`**, not `> 10` — the cap is *reached* at 10, so `>` would let an 11th slip through):

```csharp
// MovieServices/ReviewService.cs
public async Task<ReviewDto> CreateAsync(int movieId, ReviewDto dto)
{
    var movie = await uow.Movies.GetWithReviewsAsync(movieId)
                ?? throw new NotFoundException($"Movie {movieId} not found");

    if (movie.Reviews.Count >= 10)
        throw new BusinessRuleException("A movie may have at most 10 reviews.");

    if (DateTime.UtcNow.Year - movie.Year > 20 && movie.Reviews.Count >= 5)
        throw new BusinessRuleException("A movie older than 20 years may have at most 5 reviews.");

    var review = new Review
    {
        MovieId = movieId,
        ReviewerName = dto.ReviewerName,
        Comment = dto.Comment,
        Rating = dto.Rating
    };
    uow.Reviews.Add(review);
    await uow.CompleteAsync();
    dto.Id = review.Id;
    return dto;
}
```

> **Why the order is safe.** An old movie is capped at 5, so it can never reach 10 — the 5-check trips
> first. The 10-check therefore only ever bites a *recent* movie, so the two guards can stay in this order.
> (Age is by release year — `UtcNow.Year - movie.Year > 20` — so the boundary is year 2006. Coarse, but
> fine for the exercise.)

**24.3 — Seed Her with 9 reviews (already folded into Step 22.1).**
Her (2013) is recent, so the 10-cap applies to it. With 9 seeded reviews the **10th** POST succeeds and the
**11th** → 400, so you can prove the cap with two requests instead of firing ten by hand. If you seeded
before this step, re-apply Step 22.1's Her block and drop + re-run:

```bash
dotnet ef database drop -f --project MovieData --startup-project MovieApi
dotnet run --project MovieApi
```

**Verify (against the updated seed):**

| Action | Result |
|---|---|
| `POST /api/movies/6/reviews` (Her, has 9) → the 10th | **201 Created** |
| repeat that POST → the 11th | **400** — "at most 10 reviews" |
| `POST /api/movies/1/reviews` (Forrest Gump, 1994, has 2) ×3 → reaches 5 | **201** each |
| one more → the 6th on a >20-year movie | **400** — "at most 5 reviews" |

**Commit:** `feat(services): enforce review-count rules`

> **Optional:** to make the 5-cap a one-POST check too, seed an *old* movie with 4 reviews
> (e.g. Lost in Translation, 2003) — same trick as Her.

### Step 25: Enforce the Documentary caps

> Del 6 rule 5: if `Documentary` is among a movie's genres, cap **actors at 10** and **budget at
> 1,000,000** (ADR 0002). Only the **actor cap** is place-able now — `Budget` has no write path until
> Step 26, so that half moves there (note at the end). Both halves share one `IsDocumentary` helper.

**25.1 — A shared `IsDocumentary` rule helper.**
It's used by `ActorService` now and the PATCH path in Step 26, so it can't be `private` to one class.
Put it in a small shared helper that reuses the `Genres.Documentary` constant (Step 20):

```csharp
// MovieServices/MovieRules.cs
using MovieCore.Models;
namespace MovieServices;

internal static class MovieRules
{
    public static bool IsDocumentary(Movie movie) =>
        movie.Genres.Any(g => g.Name == Genres.Documentary);
}
```

**25.2 — Load `Genres` where the cap runs, then guard the actor cap.**
`AddToMovieAsync` loads the movie via `GetWithActorsAsync`, which currently includes **only `Actors`** —
so `movie.Genres` is empty there and `IsDocumentary` always returns `false` (the cap silently never fires).
Add the `Genres` include first:

```csharp
// MovieData/Repositories/MovieRepository.cs  (add the Genres include)
public Task<Movie?> GetWithActorsAsync(int movieId) => context.Movies
    .Include(m => m.Actors)
    .Include(m => m.Genres)
    .FirstOrDefaultAsync(m => m.Id == movieId);
```

Then the cap in `AddToMovieAsync`, placed **before** the Add next to the duplicate guard — `>= 10`, so
the 11th can't be added (full method shown; the duplicate guard is the one from Step 19):

```csharp
// MovieServices/ActorService.cs
public async Task AddToMovieAsync(int movieId, int actorId)
{
    var movie = await uow.Movies.GetWithActorsAsync(movieId)
                ?? throw new NotFoundException($"Movie {movieId} not found");
    var actor = await uow.Actors.GetAsync(actorId)
                ?? throw new NotFoundException($"Actor {actorId} not found");

    if (movie.Actors.Any(a => a.Id == actorId))
        throw new BusinessRuleException($"Actor {actorId} is already in movie {movieId}.");

    if (MovieRules.IsDocumentary(movie) && movie.Actors.Count >= 10)
        throw new BusinessRuleException("A documentary may have at most 10 actors.");

    movie.Actors.Add(actor);
    await uow.CompleteAsync();
}
```

**25.3 — Seed the Documentary with 10 actors (folded into Step 22.1).**
March of the Penguins (movie 5) now seeds 10 actors, so a single POST of an 11th → 400 — no need to add
nine by hand. Re-apply Step 22.1 if you seeded earlier, then drop + re-run:

```bash
dotnet ef database drop -f --project MovieData --startup-project MovieApi
dotnet run --project MovieApi
```

**Verify:**

| Action | Result |
|---|---|
| `POST /api/movies/5/actors/4` — add an 11th actor to the Documentary | **400** — "at most 10 actors" |
| `POST /api/movies/1/actors/4` — Forrest Gump is **not** a Documentary | **204** — cap doesn't apply |

**Commit:** `feat(services): enforce Documentary actor cap`

> **Budget cap deferred to Step 26.** The "Documentary budget ≤ 1,000,000" half needs a budget to guard,
> and `MovieDetails.Budget` only becomes writable through the PATCH in Step 26. Enforce it there in
> `ApplyPatchAsync`, reusing `MovieRules.IsDocumentary` — alongside Step 23's negative-budget rule. (That
> path loads `Details` + `Genres`, which the actor-cap path here doesn't need.)

### Step 26: Add PATCH for Movie + MovieDetails

> **New concept: `JsonPatchDocument`.** A list of `replace`/`add`/`remove` ops (RFC 6902). It comes from
> the `Microsoft.AspNetCore.Mvc.NewtonsoftJson` package — **not** the shared framework — because it
> depends on Newtonsoft. We patch one **flat** `MoviePatchDto` spanning `Movie` + its 1:1 `MovieDetails`.
> This step also finally owns the two **budget rules** deferred from Steps 23 and 25.

**26.1 — Add the package (to MoviePresentation) and wire Newtonsoft.**
The controller that references `JsonPatchDocument<T>` lives in **MoviePresentation**, so the package goes
there (it flows transitively to MovieApi for `AddNewtonsoftJson()`):

```bash
dotnet add MoviePresentation/MoviePresentation.csproj package Microsoft.AspNetCore.Mvc.NewtonsoftJson --version 10.0.9
```

```csharp
// MovieApi/Program.cs — chain onto the EXISTING registration; keep AddApplicationPart!
builder.Services.AddControllers()
    .AddApplicationPart(typeof(MoviePresentation.PresentationAssemblyReference).Assembly)
    .AddNewtonsoftJson();
```

> ⚠️ `AddNewtonsoftJson()` swaps the **whole API's** JSON formatter to Newtonsoft, not just this endpoint.
> Default casing stays camelCase so responses look the same; your `X-Pagination` header uses
> `System.Text.Json` explicitly and is unaffected.

**26.2 — A flat `MoviePatchDto` spanning both entities.**
Validation attributes here are what `TryValidateModel` checks **after** the patch is applied. Note
**no attribute on `Budget`** — the budget rules are `BusinessRuleException`s in the service (Steps 23/25),
so the value must reach the service rather than be caught by model validation.

```csharp
// MovieCore/DTOs/MoviePatchDto.cs
using System.ComponentModel.DataAnnotations;
namespace MovieCore.DTOs;

public class MoviePatchDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(1888, 2100)]
    public int Year { get; set; }

    [Range(1, 1000)]
    public int Duration { get; set; }

    // flattened from the 1:1 MovieDetails
    public string? Synopsis { get; set; }
    public string? Language { get; set; }
    public decimal Budget { get; set; }
}
```

**26.3 — The two service methods.** Add to the contract, then implement.

```csharp
// MovieContracts/IMovieService.cs  (add)
Task<MoviePatchDto> GetPatchModelAsync(int id);
Task ApplyPatchAsync(int id, MoviePatchDto dto);
```

`GetPatchModelAsync` builds the flat snapshot the controller patches onto (`GetWithDetailsAsync` loads
Details + Genres):

```csharp
// MovieServices/MovieService.cs
public async Task<MoviePatchDto> GetPatchModelAsync(int id)
{
    var movie = await uow.Movies.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Movie {id} not found");
    return new MoviePatchDto
    {
        Title = movie.Title,
        Year = movie.Year,
        Duration = movie.Duration,
        Synopsis = movie.Details?.Synopsis,   // null-safe: most movies have no Details row
        Language = movie.Details?.Language,
        Budget = movie.Details?.Budget ?? 0
    };
}
```

`ApplyPatchAsync` re-loads, runs the inherited business rules, writes both entities, and **creates a
`MovieDetails` row if the movie doesn't have one yet**:

```csharp
// MovieServices/MovieService.cs
public async Task ApplyPatchAsync(int id, MoviePatchDto dto)
{
    var movie = await uow.Movies.GetWithDetailsAsync(id)
                ?? throw new NotFoundException($"Movie {id} not found");

    // business rules — title (Step 23), negative budget (Step 23), Documentary cap (Step 25)
    if (await uow.Movies.TitleExistsAsync(dto.Title, id))
        throw new BusinessRuleException($"A movie titled '{dto.Title}' already exists.");
    if (dto.Budget < 0)
        throw new BusinessRuleException("Budget may not be negative.");
    if (MovieRules.IsDocumentary(movie) && dto.Budget > 1_000_000m)
        throw new BusinessRuleException("A documentary's budget may not exceed 1,000,000.");

    movie.Title = dto.Title;
    movie.Year = dto.Year;
    movie.Duration = dto.Duration;

    // MovieDetails is 1:1 and may not exist yet — create it (required props set in the initializer)
    if (movie.Details is null)
    {
        movie.Details = new MovieDetails
        {
            Synopsis = dto.Synopsis ?? string.Empty,
            Language = dto.Language ?? string.Empty,
            Budget = dto.Budget
        };
    }
    else
    {
        movie.Details.Synopsis = dto.Synopsis ?? string.Empty;
        movie.Details.Language = dto.Language ?? string.Empty;
        movie.Details.Budget = dto.Budget;
    }

    await uow.CompleteAsync();
}
```

> Title check uses `excludeId = id`, so a patch that doesn't touch the title (it still equals the
> movie's own) can't false-trip — same self-exclusion as Step 23.

**26.4 — The thin controller.**

```csharp
// MoviePresentation/Controllers/MoviesController.cs
using Microsoft.AspNetCore.JsonPatch;   // add at the top — for JsonPatchDocument<T>
// ...
[HttpPatch("{id:int}")]
public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<MoviePatchDto> patch)
{
    var dto = await services.MovieService.GetPatchModelAsync(id);   // current snapshot
    patch.ApplyTo(dto, ModelState);                                  // ModelState overload → records op errors
    if (!TryValidateModel(dto)) return ValidationProblem(ModelState);
    await services.MovieService.ApplyPatchAsync(id, dto);
    return NoContent();
}
```

**Verify (use `Content-Type: application/json-patch+json`):**

| `PATCH /api/movies/{id}` body | Result |
|---|---|
| `1`: `[{"op":"replace","path":"/budget","value":500000}]` | **204** — only Budget changes (check `/api/movies/1/details`) |
| `1`: `[{"op":"replace","path":"/budget","value":-5}]` | **400** — "Budget may not be negative." |
| `5`: `[{"op":"replace","path":"/budget","value":1000001}]` | **400** — Documentary cap (movie 5 = March of the Penguins) |
| `5`: `[{"op":"replace","path":"/budget","value":1000000}]` | **204** — at the cap, allowed; also **creates** the missing Details row |
| `2`: `[{"op":"replace","path":"/synopsis","value":"…"}]` | **204** — Shawshank had no Details; the row is created |
| `1`: `[{"op":"replace","path":"/title","value":"Her"}]` | **400** — title rule applies on PATCH too |

**Commit:** `feat(presentation): PATCH movie and details via flat JsonPatchDocument`

### Step 27: Add the test project and mock the data layer

> **New concept: NSubstitute.** Fakes `IUnitOfWork` and its repositories so service logic is tested in
> isolation — no EF, no database. Two behaviours you'll rely on: a property returning an interface
> (`uow.Movies`) is **auto-substituted recursively** (same fake instance each access — no separate setup),
> and `Task`-returning methods like `CompleteAsync()` **auto-return a completed task**, so `await` won't NRE.

**27.1 — Create the project and wire it up.**
The solution is `MovieApi.slnx`, so point `dotnet sln add` at the `.csproj`:

```bash
dotnet new xunit -n MovieServices.Tests -f net10.0
dotnet sln add MovieServices.Tests/MovieServices.Tests.csproj
dotnet add MovieServices.Tests/MovieServices.Tests.csproj reference MovieServices/MovieServices.csproj MovieContracts/MovieContracts.csproj MovieCore/MovieCore.csproj
dotnet add MovieServices.Tests/MovieServices.Tests.csproj package NSubstitute
rm MovieServices.Tests/UnitTest1.cs   # delete the scaffold stub so it doesn't count as a "passing" test
```

> **Why no `MovieData` / AutoMapper reference.** We **substitute** `IMapper` in the tests rather than build
> the real `MovieProfile` — service logic stays isolated and AutoMapper's license key never enters the test
> project. The `IMapper` *type* is still available because the `AutoMapper` package flows in **transitively**
> through the `MovieServices` reference. The real profile is already validated at API startup by
> `AssertConfigurationIsValid` (Program.cs), so it needs no separate unit test.

> **Check the scaffold.** On the .NET 10 SDK, `dotnet new xunit` may generate an **xUnit v3 /
> Microsoft.Testing.Platform** project (`.csproj` references `xunit.v3`, not `xunit`). The test code below is
> identical either way and `dotnet test` is still the runner — just confirm it restored/built.

**Verify:** `dotnet test` builds and runs; it reports **no tests** until Step 28 — that's expected, not a failure.
**Commit:** `test(services): add test project with NSubstitute`

### Step 28: Write the service tests

> Cover Brief Del 10 (≥3): the review 10-cap, the Documentary actor cap (+ a non-Documentary control),
> the error paths (duplicate title, missing id), and a happy-path create. **Match the real constructors** —
> `ReviewService(uow)` and `ActorService(uow)` take **no** mapper; only `MovieService(uow, mapper)` does.

**28.1 — Two tiny builders to keep the arrange blocks short.**

```csharp
// MovieServices.Tests/TestData.cs
using MovieCore.Models;
namespace MovieServices.Tests;

internal static class TestData
{
    public static Movie MovieWithReviews(int count, int? year = null)
    {
        var movie = new Movie { Id = 1, Title = "Test", Year = year ?? DateTime.UtcNow.Year };
        for (var i = 0; i < count; i++)
            movie.Reviews.Add(new Review { ReviewerName = $"R{i}", Comment = "c", Rating = 3 });
        return movie;
    }

    public static Movie MovieWithActors(int count, bool documentary)
    {
        var movie = new Movie { Id = documentary ? 5 : 1, Title = "Test", Year = 2005 };
        if (documentary) movie.Genres.Add(new Genre { Name = Genres.Documentary });
        for (var i = 1; i <= count; i++)
            movie.Actors.Add(new Actor { Id = i, Name = $"A{i}" });
        return movie;
    }
}
```

**28.2 — Review 10-cap (`ReviewService`, no mapper).**

```csharp
// MovieServices.Tests/ReviewServiceTests.cs
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using NSubstitute;
using Xunit;

namespace MovieServices.Tests;

public class ReviewServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenMovieAlreadyHas10Reviews_Throws()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithReviewsAsync(1).Returns(TestData.MovieWithReviews(10));   // recent year → 10-cap
        var sut = new ReviewService(uow);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => sut.CreateAsync(1, new ReviewDto { ReviewerName = "X", Comment = "c", Rating = 4 }));
    }
}
```

**28.3 — Documentary actor cap + non-Documentary control (`ActorService`, no mapper).**

```csharp
// MovieServices.Tests/ActorServiceTests.cs
using MovieCore.DomainContracts;
using MovieCore.Exceptions;
using MovieCore.Models;
using NSubstitute;
using Xunit;

namespace MovieServices.Tests;

public class ActorServiceTests
{
    [Fact]
    public async Task AddToMovieAsync_DocumentaryAt10Actors_Throws()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithActorsAsync(5).Returns(TestData.MovieWithActors(10, documentary: true));
        uow.Actors.GetAsync(99).Returns(new Actor { Id = 99, Name = "New" });
        var sut = new ActorService(uow);

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.AddToMovieAsync(5, 99));
    }

    [Fact]
    public async Task AddToMovieAsync_NonDocumentaryAt10Actors_Saves()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithActorsAsync(1).Returns(TestData.MovieWithActors(10, documentary: false));
        uow.Actors.GetAsync(99).Returns(new Actor { Id = 99, Name = "New" });
        var sut = new ActorService(uow);

        await sut.AddToMovieAsync(1, 99);        // no exception — cap doesn't apply
        await uow.Received(1).CompleteAsync();    // and it persisted
    }
}
```

**28.4 — Create errors + happy path (`MovieService`, with a substituted `IMapper`).**

```csharp
// MovieServices.Tests/MovieServiceTests.cs
using AutoMapper;                       // IMapper — available transitively via MovieServices
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;
using NSubstitute;
using Xunit;

namespace MovieServices.Tests;

public class MovieServiceTests
{
    [Fact]
    public async Task CreateAsync_DuplicateTitle_Throws()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.TitleExistsAsync("Dup", Arg.Any<int?>()).Returns(true);
        var sut = new MovieService(uow, Substitute.For<IMapper>());

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => sut.CreateAsync(new MovieCreateDto { Title = "Dup", GenreIds = [1] }));
    }

    [Fact]
    public async Task GetAsync_MissingId_ThrowsNotFound()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetAsync(404).Returns((Movie?)null);
        var sut = new MovieService(uow, Substitute.For<IMapper>());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetAsync(404));
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsMappedDtoAndSaves()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        uow.Movies.TitleExistsAsync("New", Arg.Any<int?>()).Returns(false);          // Step 23 collaborator
        uow.Genres.GetByIdsAsync(Arg.Any<IEnumerable<int>>())
           .Returns(new List<Genre> { new() { Id = 1, Name = "Drama" } });            // Step 21 collaborator

        var entity = new Movie { Title = "New", Year = 2020 };
        mapper.Map<Movie>(Arg.Any<MovieCreateDto>()).Returns(entity);
        mapper.Map<MovieDto>(entity).Returns(new MovieDto { Id = 7, Title = "New" });

        var sut = new MovieService(uow, mapper);

        var result = await sut.CreateAsync(new MovieCreateDto { Title = "New", GenreIds = [1] });

        Assert.Equal(7, result.Id);
        await uow.Received(1).CompleteAsync();
    }
}
```

> The create test substitutes `IMapper`, so it asserts the **flow** (rules pass → entity added → saved →
> mapped DTO returned), not AutoMapper's field-by-field output — that's covered at startup by
> `AssertConfigurationIsValid`. Note the extra collaborators `CreateAsync` has needed since Steps 21/23:
> `TitleExistsAsync` and `Genres.GetByIdsAsync` must both be stubbed or the method throws before mapping.

**Verify:** `dotnet test` — all 5 pass (Brief Del 10 wants ≥3).
**Commit:** `test(services): cover review cap, documentary caps, errors, create mapping`

---

## Phase 2 — Stretch Goals

> Optional/bonus work beyond the brief's mandatory requirements — the review trimmer (Del 6.4 extra) and
> Del 11 experimentation. **Steps 32–37 (set B)** add the three cross-cutting features from the later
> lectures (0625–0629): **logging**, **API versioning**, and **API documentation**. They are independent
> of the brief and of each other; build them in order (logging → versioning → documentation), because the
> multi-version Swagger docs in Step 36 read the version descriptions that Step 33 sets up.

### Step 29: Add CreatedAt to Review

> The trimmer (Step 30) removes the **oldest** reviews, so each `Review` needs a timestamp to order by
> (ADR 0004, builds on Step 24). Three parts: the field + migration, **staggered** seed values (so
> "oldest" is meaningful and the trimmer has a real target), and surfacing it on the read DTO so you can
> actually see it.

**29.1 — Add the field and migrate.**

```csharp
// MovieCore/Models/Review.cs
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

> `= DateTime.UtcNow` is a **C# initializer, not a SQL default**: new reviews (API or seed) get a real
> timestamp at construction, but it doesn't back-fill existing rows. `ReviewService.CreateAsync` builds
> `new Review { … }` without touching `CreatedAt`, so the **server** assigns it — a client can't set it.

```bash
dotnet ef migrations add ReviewCreatedAt --project MovieData --startup-project MovieApi
```

Because 29.2 changes the seed (and the new column doesn't back-fill old rows with sensible values),
**drop and re-seed** rather than just `database update`:

```bash
dotnet ef database drop -f --project MovieData --startup-project MovieApi
dotnet run --project MovieApi
```

**29.2 — Give an old movie 8 staggered reviews (folded into Step 22.1).**
The trimmer targets movies **>20 years old with >5 reviews** — but no seeded movie qualified (the old
ones had ≤3 reviews; Her has 9 but is recent). Make **The Shawshank Redemption** (1994, movie 2) the
target by seeding **8 reviews with explicit, decreasing `CreatedAt`**, so the 5 newest are deterministic
and the trimmer removes the oldest 3. Replace Shawshank's `Reviews` block (shown in 29 here; it lives in
the Step 22.1 seed):

```csharp
// MovieData/Extensions/SeedDataExtensions.cs — Shawshank's Reviews (8, oldest → newest)
Reviews =
{
    new Review { ReviewerName = "Cara",   Comment = "Masterpiece.",   Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-8) },
    new Review { ReviewerName = "Dan",    Comment = "Hopeful.",       Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-7) },
    new Review { ReviewerName = "Eve",    Comment = "Slow start.",    Rating = 3, CreatedAt = DateTime.UtcNow.AddDays(-6) },
    new Review { ReviewerName = "Greta",  Comment = "Unforgettable.", Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-5) },
    new Review { ReviewerName = "Hans",   Comment = "A classic.",     Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-4) },
    new Review { ReviewerName = "Ingrid", Comment = "Powerful.",      Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-3) },
    new Review { ReviewerName = "Jonas",  Comment = "Moving.",        Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-2) },
    new Review { ReviewerName = "Karin",  Comment = "Brilliant.",     Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-1) }
}
```

> Other movies' reviews keep the default `CreatedAt` (set by the initializer) — only the trimmer's target
> needs staggering. Seeding 8 reviews on an old movie deliberately exceeds the 5-review **write** cap from
> Step 24: that cap guards new POSTs, not seed data, and clearing this backlog is exactly the trimmer's
> job in Step 30.

**29.3 — Surface `CreatedAt` on the read DTO (so the verify is checkable via the API).**

```csharp
// MovieCore/DTOs/ReviewDto.cs  (add — output only; create ignores it)
public DateTime CreatedAt { get; set; }
```

```csharp
// MovieServices/ReviewService.cs — in GetByMovieIdAsync's Select, add the field
CreatedAt = r.CreatedAt,
```

```csharp
// MovieServices/MovieService.cs — in GetDetailsAsync's Reviews Select, add the field
CreatedAt = r.CreatedAt,
```

**Verify:**
- `GET /api/movies/2/reviews` → 8 reviews, each with a distinct `createdAt`; oldest is Cara (~8 days ago).
- `GET /api/movies/1/reviews` (Forrest Gump) → `createdAt` ≈ seed time (default initializer).

**Commit:** `feat(core): timestamp reviews with CreatedAt`

### Step 30: Add the idempotent review-trimmer background service

> **New concept: `BackgroundService` + `IServiceScopeFactory`.** A hosted worker runs on a loop,
> independent of HTTP. It's registered as a **singleton**, so it **can't** hold a scoped `IUnitOfWork` —
> it must open a **scope per run** and resolve the unit of work inside it (ADR 0004). Builds on Steps 24 & 29.

**30.1 — Add a query that loads every movie with its reviews.**
The trimmer reads each movie's `Reviews` and `Year`. `GetAllAsync` includes Genres + Actors but **not**
Reviews, so `movie.Reviews` would be empty and nothing would trim. Add a dedicated query:

```csharp
// MovieCore/DomainContracts/IMovieRepository.cs  (add)
Task<IEnumerable<Movie>> GetAllWithReviewsAsync();
```

```csharp
// MovieData/Repositories/MovieRepository.cs  (add)
public async Task<IEnumerable<Movie>> GetAllWithReviewsAsync() =>
    await context.Movies.Include(m => m.Reviews).ToListAsync();
```

**30.2 — The worker itself.**
Note the constructor takes `IServiceScopeFactory`, **not** `IUnitOfWork` — that's the whole point. The
per-tick body is wrapped in `try/catch` so a transient failure logs and retries instead of killing the
worker, and the trim is idempotent: a movie already at ≤5 reviews is skipped.

```csharp
// MovieApi/BackgroundServices/ReviewTrimmer.cs
using MovieCore.DomainContracts;   // IUnitOfWork — the rest comes from the web SDK's implicit usings

namespace MovieApi.BackgroundServices;

public class ReviewTrimmer(IServiceScopeFactory scopeFactory, ILogger<ReviewTrimmer> logger) : BackgroundService
{
    private const int KeepNewest = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TrimAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Review-trim pass failed");   // don't let one bad pass kill the worker
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task TrimAsync()
    {
        // a singleton can't capture a scoped IUnitOfWork → open a scope per run and resolve inside it
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoffYear = DateTime.UtcNow.Year - 20;   // "older than 20 years" — same definition as Step 24
        var movies = await uow.Movies.GetAllWithReviewsAsync();

        foreach (var movie in movies)
        {
            if (movie.Year >= cutoffYear || movie.Reviews.Count <= KeepNewest)
                continue;   // recent, or already within the cap → nothing to do (this is the idempotency)

            var toRemove = movie.Reviews
                .OrderByDescending(r => r.CreatedAt)   // newest first
                .Skip(KeepNewest)                      // keep the 5 newest, remove everything older
                .ToList();

            foreach (var review in toRemove)
                uow.Reviews.Remove(review);

            logger.LogInformation("Trimmed {Count} old review(s) from movie {MovieId}", toRemove.Count, movie.Id);
        }

        await uow.CompleteAsync();
    }
}
```

```csharp
// MovieApi/Program.cs
using MovieApi.BackgroundServices;   // for ReviewTrimmer
// ...
builder.Services.AddHostedService<ReviewTrimmer>();
```

> **It runs at startup.** `AddHostedService` starts `ExecuteAsync` as the app boots, and the loop calls
> `TrimAsync` immediately (no initial delay). So a freshly re-seeded Shawshank is trimmed within moments
> of `dotnet run`.

**Verify:**

| Check | Result |
|---|---|
| Re-seed, `dotnet run`, watch the console | log line: `Trimmed 3 old review(s) from movie 2` |
| `GET /api/movies/2/reviews` (Shawshank) shortly after startup | **5** reviews — the newest (Greta, Hans, Ingrid, Jonas, Karin); Cara/Dan/Eve gone |
| Restart and let it tick again (idempotency) | movie 2 now has 5 → skipped, **nothing** removed, no log line for it |
| `GET /api/movies/6/reviews` (Her, 2013) | untouched — Her is recent, so the trimmer ignores it even with 9 reviews |

> **Reconciles with Step 29.** Shawshank's **8** is the seed/pre-trim state (what Step 29 verifies before
> this worker exists); **5** is the steady state once the trimmer is running. If you want to see the 8
> first, query before the worker's first pass, or check it on the build at Step 29.

**Commit:** `feat(api): add idempotent review-trimmer background service`

### Step 31: (Optional) Demonstrate the Result<T> alternative on one controller

> Del 9 invites showing **both** error styles. The house default stays exceptions + `IExceptionHandler`
> (ADR 0003); here we convert **one slice — the Actor slice — end to end** to the `Result<T>` style so the
> trade-off is visible side by side: the Actor service returns success/failure **in-band** (no throwing),
> and `ActorsController` translates a failed `Result` into `ProblemDetails` itself, while Movies and Reviews
> still throw and flow through the central handler. Builds on Steps 14, 19, 25, 28.

> **Why a whole slice, not one method.** The controller can only translate a `Result` if the service
> *returns* one instead of throwing. So this isn't a controller-only change: the `IActorService` contract,
> `ActorService`, the controller, **and the Actor unit test** all move together. That's the point — it
> shows the style is a layering decision, not a controller trick.

**31.1 — Add the `Result` type in MovieCore.**
It must live in **MovieCore** (referenced by both MovieServices and, transitively, MoviePresentation) so the
service can return it and the controller can read it. Non-generic `Result` for void operations; `Result<T>`
carries a value on success.

```csharp
// MovieCore/Result.cs
namespace MovieCore;

public enum ErrorType { None, NotFound, BusinessRule }

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, ErrorType errorType, string? error)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Error = error;
    }

    public static Result Success() => new(true, ErrorType.None, null);
    public static Result NotFound(string error) => new(false, ErrorType.NotFound, error);
    public static Result BusinessRule(string error) => new(false, ErrorType.BusinessRule, error);

    // value-carrying siblings (T inferred from the argument on Success)
    public static Result<T> Success<T>(T value) => new(value, true, ErrorType.None, null);
    public static Result<T> NotFound<T>(string error) => new(default!, false, ErrorType.NotFound, error);
    public static Result<T> BusinessRule<T>(string error) => new(default!, false, ErrorType.BusinessRule, error);
}

public sealed class Result<T> : Result
{
    public T Value { get; }

    internal Result(T value, bool isSuccess, ErrorType errorType, string? error)
        : base(isSuccess, errorType, error) => Value = value;
}
```

> The `Result<T>` constructor is `internal`, but the static factories that call it live on `Result` in the
> **same assembly (MovieCore)**, so they construct it fine; callers in MovieServices use only the factories.
> `Result.NotFound<ActorDto>("…")` needs the explicit type argument (there's no value to infer `T` from);
> `Result.Success(dto)` infers it.

**31.2 — Convert the Actor contract and service to return `Result` (stop throwing).**

```csharp
// MovieContracts/IActorService.cs
using MovieCore;
using MovieCore.DTOs;

namespace MovieContracts;

public interface IActorService
{
    Task<Result<IEnumerable<ActorDto>>> GetAllAsync();
    Task<Result<ActorDto>> GetAsync(int id);
    Task<Result<ActorDto>> CreateAsync(ActorDto dto);
    Task<Result> UpdateAsync(int id, ActorDto dto);
    Task<Result> AddToMovieAsync(int movieId, int actorId);
}
```

```csharp
// MovieServices/ActorService.cs
using MovieContracts;
using MovieCore;                 // Result, ErrorType
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;
// NOTE: remove `using MovieCore.Exceptions;` — this service no longer throws.

namespace MovieServices;

public class ActorService(IUnitOfWork uow) : IActorService
{
    public async Task<Result<IEnumerable<ActorDto>>> GetAllAsync()
    {
        var actors = await uow.Actors.GetAllAsync();
        var dtos = actors.Select(a => new ActorDto { Id = a.Id, Name = a.Name, BirthYear = a.BirthYear })
                         .ToList();
        return Result.Success<IEnumerable<ActorDto>>(dtos);
    }

    public async Task<Result<ActorDto>> GetAsync(int id)
    {
        var actor = await uow.Actors.GetAsync(id);
        if (actor is null) return Result.NotFound<ActorDto>($"Actor {id} not found");

        return Result.Success(new ActorDto { Id = actor.Id, Name = actor.Name, BirthYear = actor.BirthYear });
    }

    public async Task<Result<ActorDto>> CreateAsync(ActorDto dto)
    {
        var actor = new Actor { Name = dto.Name, BirthYear = dto.BirthYear };
        uow.Actors.Add(actor);
        await uow.CompleteAsync();
        dto.Id = actor.Id;
        return Result.Success(dto);
    }

    public async Task<Result> UpdateAsync(int id, ActorDto dto)
    {
        var actor = await uow.Actors.GetAsync(id);
        if (actor is null) return Result.NotFound($"Actor {id} not found");

        actor.Name = dto.Name;
        actor.BirthYear = dto.BirthYear;
        await uow.CompleteAsync();
        return Result.Success();
    }

    public async Task<Result> AddToMovieAsync(int movieId, int actorId)
    {
        var movie = await uow.Movies.GetWithActorsAsync(movieId);
        if (movie is null) return Result.NotFound($"Movie {movieId} not found");

        var actor = await uow.Actors.GetAsync(actorId);
        if (actor is null) return Result.NotFound($"Actor {actorId} not found");

        if (movie.Actors.Any(a => a.Id == actorId))
            return Result.BusinessRule($"Actor {actorId} is already in movie {movieId}.");

        if (MovieRules.IsDocumentary(movie) && movie.Actors.Count >= 10)
            return Result.BusinessRule("A documentary can only have 10 actors.");

        movie.Actors.Add(actor);
        await uow.CompleteAsync();
        return Result.Success();
    }
}
```

**31.3 — Translate `Result` to `ProblemDetails` in `ActorsController`.**
Use **guard-style returns**, not a ternary: `return ok ? NoContent() : ToProblem(result);` does **not** compile
(CS0173 — no implicit conversion between `NoContentResult` and `ObjectResult`). The `ToProblem` helper reuses
`ControllerBase.Problem(...)`, which returns the same `ProblemDetails` body the central handler produces — so
clients can't tell the two error styles apart.

```csharp
// MoviePresentation/Controllers/ActorsController.cs
using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore;            // Result, ErrorType
using MovieCore.DTOs;

namespace MoviePresentation.Controllers;

[ApiController]
[Route("api")]
public class ActorsController(IServiceManager services) : ControllerBase
{
    [HttpGet("actors")]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors()
    {
        var result = await services.ActorService.GetAllAsync();
        if (!result.IsSuccess) return ToProblem(result);
        return Ok(result.Value);
    }

    [HttpGet("actors/{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id)
    {
        var result = await services.ActorService.GetAsync(id);
        if (!result.IsSuccess) return ToProblem(result);
        return Ok(result.Value);
    }

    [HttpPost("actors")]
    public async Task<ActionResult<ActorDto>> CreateActor(ActorDto dto)
    {
        var result = await services.ActorService.CreateAsync(dto);
        if (!result.IsSuccess) return ToProblem(result);
        return CreatedAtAction(nameof(GetActor), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("actors/{id:int}")]
    public async Task<IActionResult> UpdateActor(int id, ActorDto dto)
    {
        var result = await services.ActorService.UpdateAsync(id, dto);
        if (!result.IsSuccess) return ToProblem(result);
        return NoContent();
    }

    [HttpPost("movies/{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> AddActorToMovie(int movieId, int actorId)
    {
        var result = await services.ActorService.AddToMovieAsync(movieId, actorId);
        if (!result.IsSuccess) return ToProblem(result);
        return NoContent();
    }

    private ObjectResult ToProblem(Result result) =>
        Problem(detail: result.Error, statusCode: result.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.BusinessRule => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        });
}
```

> `UpdateActor` changes its declared return type from `ActionResult` to `IActionResult` so the guard's
> `return ToProblem(result);` (an `ObjectResult`) and `return NoContent();` share a return type cleanly.
> The typed actions return `ActionResult<T>`; `ObjectResult`/`OkObjectResult`/`CreatedAtActionResult` all
> convert to it implicitly, so the guard pattern compiles without casts.

**31.4 — Update the Actor unit test (it asserted a throw).**
Step 28's `AddToMovieAsync_DocumentaryAt10Actors_Throws` no longer compiles against a non-throwing service —
assert on the **`Result`** instead, and confirm nothing was persisted:

```csharp
// MovieServices.Tests/ActorServiceTests.cs
using MovieCore;               // Result, ErrorType
using MovieCore.DomainContracts;
using MovieCore.Models;
using NSubstitute;
// NOTE: remove `using MovieCore.Exceptions;`

namespace MovieServices.Tests;

public class ActorServiceTests
{
    [Fact]
    public async Task AddToMovieAsync_DocumentaryAt10Actors_ReturnsBusinessRule()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithActorsAsync(5).Returns(TestData.MovieWithActors(10, documentary: true));
        uow.Actors.GetAsync(99).Returns(new Actor { Id = 99, Name = "New" });
        var sut = new ActorService(uow);

        var result = await sut.AddToMovieAsync(5, 99);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BusinessRule, result.ErrorType);
        await uow.DidNotReceive().CompleteAsync();   // rejected before saving
    }

    [Fact]
    public async Task AddToMovieAsync_NonDocumentaryAt10Actors_Saves()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithActorsAsync(1).Returns(TestData.MovieWithActors(10, documentary: false));
        uow.Actors.GetAsync(99).Returns(new Actor { Id = 99, Name = "New" });
        var sut = new ActorService(uow);

        var result = await sut.AddToMovieAsync(1, 99); // cap doesn't apply

        Assert.True(result.IsSuccess);
        await uow.Received(1).CompleteAsync();         // and it persisted
    }
}
```

**Verify:**

| Check | Result |
|---|---|
| `GET /api/actors` / `GET /api/actors/1` | 200 + body (unchanged behaviour, now via `Result`) |
| `GET /api/actors/9999` | **404 `ProblemDetails`** — produced by `ToProblem`, *not* the central handler |
| `POST /api/movies/5/actors/4` (March of the Penguins, 10 actors) | **400** "A documentary can only have 10 actors." via `Result` |
| `POST /api/movies/1/actors/1` twice | 1st **204**, 2nd **400** "already in movie" via `Result` |
| Any Movies/Reviews error (e.g. `GET /api/movies/9999`) | still **404** through `IExceptionHandler` — the rest of the app is unchanged |
| `dotnet test` | green — the Actor test now asserts on the `Result`, not a thrown exception |

**Commit:** `feat(presentation): demonstrate Result<T> error style on the Actor slice`

---

> **Set B — cross-cutting features (lectures 0625–0629).** The steps below are *not* in the Övning 6 brief;
> they come from the API-versioning (0625), documentation (0626), and logging (0629) lectures and from the
> class repo <https://github.com/Lexicon-LTU-VT-2026/WebAPI-Versioning>. Design decisions: **ADR 0005**
> (URL-segment versioning, v2 reshapes Genre) and **ADR 0006** (Swashbuckle + Swagger UI + Scalar). Each
> new concern is registered through a small **DI extension method** so `Program.cs` stays a thin
> composition root.

### Step 32: Add structured logging across the stack

> **New concept: `ILogger<T>` + structured logging.** ASP.NET Core's built-in logger is injected by DI as
> `ILogger<TheClass>` (the `T` becomes the log *category*). Always log with a **message template**
> (`"Created movie {Id}"`, `id`) — not string interpolation — so providers can capture `Id` as a real
> field. Builds on Step 14 (the exception handler) and Step 30 (the trimmer already logs this way).

**32.1 — Let the service layer see `ILogger<T>`.** `MovieServices` is a plain class library with no
ASP.NET framework reference, so `ILogger<T>` (in `Microsoft.Extensions.Logging.Abstractions`) isn't
guaranteed to resolve. Add the package.

```bash
dotnet add MovieServices/MovieServices.csproj package Microsoft.Extensions.Logging.Abstractions --version 10.0.9
```

> If `ILogger<T>` already resolves in `MovieServices` (it can arrive transitively), this is a no-op —
> but adding it makes the dependency explicit, the same way Step 13 made AutoMapper explicit.

**32.2 — Inject the logger into `MovieService` and log the writes + rule rejections.** Add a logger
parameter to the primary constructor, then log at the meaningful points — *not* on every read.

```csharp
// MovieServices/MovieService.cs
using Microsoft.Extensions.Logging;   // ← add
// ...
public class MovieService(IUnitOfWork uow, IMapper mapper, ILogger<MovieService> logger) : IMovieService
{
    // ... in CreateAsync, after await uow.CompleteAsync():
    logger.LogInformation($"Created movie {movie.Id} '{movie.Title}'");   // ← mistake: use a template ("Created movie {Id} {Title}", movie.Id, movie.Title) — interpolation loses the structured fields

    // ... in CreateAsync, replace the genre-missing throw's lead-in:
    logger.LogWarning("Rejected create for '{Title}': no genres supplied", dto.Title);
}
```

**32.3 — Log unhandled (500-class) errors in the exception handler.** Domain exceptions are *expected*
(404/400) — log them at most at `Debug`. Anything that falls through to the `_ =>` 500 branch is a real
fault and deserves `LogError` with the exception.

```csharp
// MovieApi/ExceptionHandling/DomainExceptionHandler.cs
public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    // ... after computing `status`, before writing the ProblemDetails:
    if (status == StatusCodes.Status500InternalServerError)
        logger.LogError(exception, "Unhandled exception");
}
```

**32.4 — Turn on HTTP request logging and set per-category levels.** `AddHttpLogging` + `UseHttpLogging`
log each request/response through the built-in middleware. Wrap the registration in a tiny extension so
`Program.cs` reads cleanly.

```csharp
// MovieApi/Extensions/LoggingExtensions.cs
namespace MovieApi.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddApplicationLogging(this IServiceCollection services) =>
        services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders);
}
```

```csharp
// MovieApi/Program.cs
builder.Services.AddApplicationLogging();   // near the other registrations
// ...
app.UseHttpLogging();                        // early in the pipeline, after UseExceptionHandler()
```

```jsonc
// MovieApi/appsettings.Development.json  — demote framework noise, spotlight your service
"LogLevel": {
  "Default": "Information",
  "Microsoft.AspNetCore": "Warning",
  "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information",
  "MovieServices.MovieService": "Debug"
}
```

**Verify:** `dotnet run`; `POST /api/movies` → console shows `Created movie 11 '…'`; an HTTP-logging line
appears per request; `GET /api/movies` (no genres on a create) → a `warn` line. Force a 500 (e.g. stop SQL
Server) → one `fail` line with a stack trace, but the client still gets a clean `ProblemDetails`.
**Commit:** `feat(api): add ILogger structured logging and HTTP request logging`

### Step 33: Install Asp.Versioning and add the versioning DI extension

> **New concept: `Asp.Versioning`.** Two packages: `Asp.Versioning.Mvc` (versions controllers) and
> `Asp.Versioning.Mvc.ApiExplorer` (so Swagger can discover and group each version). We choose **URL-segment**
> versioning (`/api/v{version}/...`) — ADR 0005. This step is pure setup; routes change in Step 34.

**33.1 — Add the packages to `MovieApi`** (the composition root wires versioning; the attributes used in
`MoviePresentation` come transitively).

```bash
dotnet add MovieApi/MovieApi.csproj package Asp.Versioning.Mvc --version 10.0.0
dotnet add MovieApi/MovieApi.csproj package Asp.Versioning.Mvc.ApiExplorer --version 10.0.0
```

> `MoviePresentation` needs the `[ApiVersion]` / `[MapToApiVersion]` attributes too. They live in
> `Asp.Versioning.Abstractions`, which flows in transitively via `Asp.Versioning.Mvc` once `MovieApi`
> references the controllers' assembly — if the attribute won't resolve there, add
> `Asp.Versioning.Mvc` to `MoviePresentation` as well.

**33.2 — The versioning extension.** `AddMvc()` between `AddApiVersioning()` and `AddApiExplorer()` is what
binds versioning to controllers — omit it and the ApiExplorer grouping silently does nothing.

```csharp
// MovieApi/Extensions/ApiVersioningExtensions.cs
using Asp.Versioning;

namespace MovieApi.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfigured(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;   // v1 is the assumed default
                options.ReportApiVersions = true;                     // api-supported-versions response header
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";        // -> "v1", "v2"
                options.SubstituteApiVersionInUrl = true;  // fill {version} in the documented paths
            });

        return services;
    }
}
```

```csharp
// MovieApi/Program.cs
using MovieApi.Extensions;
// ...
builder.Services.AddApiVersioningConfigured();
```

**Verify:** `dotnet build` green; `dotnet run` boots. No route changed yet — that's Step 34.
**Commit:** `feat(api): configure URL-segment API versioning`

### Step 34: Move every controller under /api/v1

> The whole API becomes consistently versioned (ADR 0005). Each controller is declared `[ApiVersion("1.0")]`
> and its route gains the `v{version:apiVersion}` segment. This **breaks** the old `/api/...` routes — that's
> expected; `/api/v1/...` is the new shape and `v1` is the assumed default.

**34.1 — Version `MoviesController`.** Add the attribute and the route segment; move the file into a `V1`
folder + namespace so a `V2` sibling can exist in Step 35.

```csharp
// MoviePresentation/Controllers/V1/MoviesController.cs   (moved from Controllers/)
using Asp.Versioning;
// ...
namespace MoviePresentation.Controllers.V1;   // ← was MoviePresentation.Controllers

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/movies")]   // ← was [Route("api/movies")]
public class MoviesController(IServiceManager services) : ControllerBase
```

**34.2 — Version `ActorsController` and `ReviewsController`.** Both use `[Route("api")]` with the segment on
each action, so only the class-level route + attribute change.

```csharp
// MoviePresentation/Controllers/V1/ActorsController.cs  (and ReviewsController.cs)
using Asp.Versioning;
// ...
namespace MoviePresentation.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]   // ← was [Route("api")]; action templates (e.g. "actors") unchanged
public class ActorsController(IServiceManager services) : ControllerBase
```

> **Why `nameof(GetMovie)` in `CreatedAtAction` still works:** `SubstituteApiVersionInUrl` and the route's
> `{version}` token resolve from the current request's version, so the generated `Location` points at
> `/api/v1/movies/{id}`. No change needed in the action bodies.

**Verify:** `dotnet run`; `GET /api/v1/movies`, `GET /api/v1/actors`, `GET /api/v1/movies/1/reviews` all
work; the old `GET /api/movies` now **404**s; responses carry an `api-supported-versions: 1.0` header.
**Commit:** `refactor(presentation): move all controllers under /api/v1`

### Step 35: Add a v2 of the Movies list (the breaking change)

> v1 keeps the legacy joined `Genre` string; **v2** exposes `Genres` as an array — the honest shape now that
> genres are many-to-many (ADR 0002, 0005). This is the lecture's `firstName → givenName` break, contained to
> a new endpoint so v1 clients are untouched. We version just the **list** endpoint to keep it focused.

**35.1 — The v2 read DTO.**

```csharp
// MovieCore/DTOs/MovieDtoV2.cs
namespace MovieCore.DTOs;

public class MovieDtoV2
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<string> Genres { get; set; } = [];   // ← v2: array, not a joined string
    public int Duration { get; set; }
}
```

**35.2 — Map it, and add a service method.** AutoMapper projects `Genres` from the navigation; the service
gets a v2 list method on the existing contract.

```csharp
// MovieData/Mapping/MovieProfile.cs   (add inside the ctor)
CreateMap<Movie, MovieDtoV2>()
    .ForMember(d => d.Genres, o => o.MapFrom(s => s.Genres.Select(g => g.Name)));
```

```csharp
// MovieContracts/IMovieService.cs   (add)
Task<IEnumerable<MovieDtoV2>> GetAllV2Async(string? genre, int? year, string? actor);
```

```csharp
// MovieServices/MovieService.cs   (add; reuses the existing ApplyFilters)
public async Task<IEnumerable<MovieDtoV2>> GetAllV2Async(string? genre, int? year, string? actor) =>
    mapper.Map<IEnumerable<MovieDtoV2>>(ApplyFilters(await uow.Movies.GetAllAsync(), genre, year, actor));
```

**35.3 — The v2 controller.** A separate class in a `V2` namespace (the class name `MoviesController` may
repeat across namespaces). It declares only `2.0` and only the one reshaped action.

```csharp
// MoviePresentation/Controllers/V2/MoviesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MovieContracts;
using MovieCore.DTOs;

namespace MoviePresentation.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/movies")]
public class MoviesController(IServiceManager services) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDtoV2>>> GetMovies(
        [FromQuery] string? genre, [FromQuery] int? year, [FromQuery] string? actor) =>
        Ok(await services.MovieService.GetAllV2Async(genre, year, actor));
}
```

**Verify:** `dotnet run`; `GET /api/v1/movies` → items with `"genre": "Drama, Crime"`; `GET /api/v2/movies`
→ the same movies with `"genres": ["Drama","Crime"]`; `GET /api/v3/movies` → 400 (unsupported version).
**Commit:** `feat(presentation): add v2 movies list exposing genres as an array`

### Step 36: Swap native OpenAPI for Swashbuckle + version-aware Swagger UI

> **New concept: Swashbuckle with XML comments + one doc per version.** We replace .NET 10's native
> `AddOpenApi`/`MapOpenApi` with `Swashbuckle.AspNetCore` (ADR 0006), feed it the projects' XML doc files,
> and generate **one Swagger document per API version** from `IApiVersionDescriptionProvider` instead of
> hardcoding `"v1"`/`"v2"`. Builds on Step 33.

**36.1 — Replace the package and the calls.** Remove `Microsoft.AspNetCore.OpenApi`, add Swashbuckle.

```bash
dotnet remove MovieApi/MovieApi.csproj package Microsoft.AspNetCore.OpenApi
dotnet add    MovieApi/MovieApi.csproj package Swashbuckle.AspNetCore --version 10.2.3
```

```csharp
// MovieApi/Program.cs   (delete these two lines)
builder.Services.AddOpenApi();   // ← remove
app.MapOpenApi();                // ← remove (inside the IsDevelopment block)
```

**36.2 — Turn on XML doc generation where the documented types live** — controllers (`MoviePresentation`)
and DTOs (`MovieCore`) — and silence the "missing comment" warning (lecture's CS1591 note).

```xml
<!-- MoviePresentation/MoviePresentation.csproj AND MovieCore/MovieCore.csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>   <!-- don't fail on undocumented public members -->
</PropertyGroup>
```

**36.3 — Generate a Swagger doc per version.** An `IConfigureOptions<SwaggerGenOptions>` reads the version
descriptions and registers a `SwaggerDoc` for each — so a future `v3` needs *no* Program.cs change.

```csharp
// MovieApi/Extensions/SwaggerOptionsConfigurator.cs
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MovieApi.Extensions;

public class SwaggerOptionsConfigurator(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var d in provider.ApiVersionDescriptions)
            options.SwaggerDoc(d.GroupName, new OpenApiInfo
            {
                Title = "Movie API",
                Version = d.ApiVersion.ToString(),
                Description = d.IsDeprecated ? "This API version is deprecated." : null
            });
    }
}
```

**36.4 — The documentation extension** (registers SwaggerGen + the XML files + the configurator).

```csharp
// MovieApi/Extensions/ApiDocumentationExtensions.cs
using System.Reflection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MovieApi.Extensions;

public static class ApiDocumentationExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptionsConfigurator>();
        services.AddSwaggerGen(options =>
        {
            foreach (var assembly in new[] { "MoviePresentation", "MovieCore" })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly}.xml");
                if (File.Exists(xml)) options.IncludeXmlComments(xml);
            }
        });
        return services;
    }
}
```

```csharp
// MovieApi/Program.cs
builder.Services.AddApiDocumentation();   // replaces AddOpenApi()
// ...
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // serves /swagger/v1/swagger.json, /swagger/v2/swagger.json
    app.UseSwaggerUI(options =>
    {
        foreach (var d in app.DescribeApiVersions())
            options.SwaggerEndpoint($"/swagger/{d.GroupName}/swagger.json", $"Movie API {d.GroupName}");
    });
}
```

**Verify:** `dotnet run`; `/swagger` shows a version dropdown with **v1** and **v2**; the v1 doc lists movies
(with `genre`), actors and reviews; the v2 doc lists the movies list returning `genres[]`.
**Commit:** `feat(api): document the API with Swashbuckle and per-version Swagger UI`

### Step 37: Add Scalar UI and sample XML comments

> **New concept: Scalar.** A modern API-reference UI that renders the same OpenAPI documents Swashbuckle
> already serves — offered alongside classic Swagger UI (ADR 0006). We also add real `<summary>`/`<response>`
> comments to one slice so both UIs show them.

**37.1 — Add Scalar and point it at the Swagger documents.**

```bash
dotnet add MovieApi/MovieApi.csproj package Scalar.AspNetCore --version 2.16.6
```

```csharp
// MovieApi/Program.cs   (inside the IsDevelopment block, after UseSwaggerUI)
using Scalar.AspNetCore;
// ...
app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
    foreach (var d in app.DescribeApiVersions())
        options.AddDocument(d.GroupName, d.GroupName);
});
```

**37.2 — Document one slice so the comments actually surface.** XML comments on the action + a
`ProducesResponseType` (so the UIs list the 404) — apply the same pattern to the rest over time.

```csharp
// MoviePresentation/Controllers/V1/MoviesController.cs   (on GetMovie)
/// <summary>Get a single movie by its id.</summary>
/// <param name="id">The movie's unique identifier.</param>
/// <response code="200">The movie was found.</response>
/// <response code="404">No movie exists with that id.</response>
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[HttpGet("{id:int}")]
public async Task<ActionResult<MovieDto>> GetMovie(int id) =>
```

```csharp
// MovieCore/DTOs/MovieDtoV2.cs   (a DTO comment shows in the schema section)
/// <summary>Movie as exposed by API v2 — genres are a list, not a joined string.</summary>
public class MovieDtoV2
```

**Verify:** `dotnet run`; `/scalar/v1` renders the reference UI; `GET /api/v1/movies/{id}` shows the summary,
the `id` parameter description, and both 200/404 responses; the v2 schema shows the DTO summary. Swagger UI
(Step 36) shows the same comments.
**Commit:** `feat(api): add Scalar reference UI and XML doc comments`

