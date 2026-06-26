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

> **New concept: `FrameworkReference` + `AddApplicationPart`.** A controller in a *separate* class library needs the ASP.NET framework reference, and the API must be told to scan that assembly for controllers — otherwise its routes silently 404.

```xml
<!-- MoviePresentation/MoviePresentation.csproj -->
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

```csharp
// MovieApi/Program.cs
builder.Services.AddControllers()
    .AddApplicationPart(typeof(MoviePresentation.Controllers.MoviesController).Assembly);
```

**Verify:** `dotnet build` passes (controllers move in the next step).
**Commit:** `chore(presentation): enable controller hosting from the library`

### Step 13: Add AutoMapper and a Movie profile

> **New concept: AutoMapper.** Maps entities ↔ DTOs from a profile. Profiles live in `MovieData`; services consume `IMapper`.

```bash
dotnet add MovieData/MovieData.csproj package AutoMapper
dotnet add MovieApi/MovieApi.csproj   package AutoMapper   # for the DI registration
```

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
        CreateMap<MovieCreateDto, Movie>();
    }
}
```

```csharp
// MovieApi/Program.cs
builder.Services.AddAutoMapper(cfg => { }, typeof(MovieData.Mapping.MovieProfile).Assembly);
```

**Verify:** `dotnet run` boots; AutoMapper config validates at startup.
**Commit:** `feat(data): add AutoMapper MovieProfile`

### Step 14: Add the exception hierarchy and IExceptionHandler

> **New concept: `IExceptionHandler` + `ProblemDetails`.** Services can't call `NotFound()`. They throw domain exceptions; one handler maps each to a status code + `ProblemDetails` body (ADR 0003). This is brief **Del 9**, set up early so services can throw from day one.

```csharp
// MovieCore/Exceptions/DomainException.cs
namespace MovieCore.Exceptions;
public abstract class DomainException(string message) : Exception(message);
public sealed class NotFoundException(string message) : DomainException(message);
public sealed class BusinessRuleException(string message) : DomainException(message);
```

```csharp
// MovieApi/ExceptionHandling/DomainExceptionHandler.cs   (implements IExceptionHandler)
public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
{
    var status = ex switch
    {
        NotFoundException     => StatusCodes.Status404NotFound,
        BusinessRuleException => StatusCodes.Status400BadRequest,
        _                     => StatusCodes.Status500InternalServerError,
    };
    await Results.Problem(detail: ex.Message, statusCode: status).ExecuteAsync(ctx);
    return true;
}
```

```csharp
// MovieApi/Program.cs
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
// ... after build: app.UseExceptionHandler();
```

**Verify:** `dotnet run`; hitting a not-yet-handled path returns a `ProblemDetails` JSON body, not a stack trace.
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

```csharp
// MoviePresentation/Controllers/MoviesController.cs   (moved out of MovieApi)
namespace MoviePresentation.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController(IServiceManager services) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDto>> GetMovie(int id)
        => Ok(await services.MovieService.GetAsync(id));   // no DbContext/UoW/IMapper here
}
```

**Verify:** `dotnet run`; `GET /api/movies/1` works; grep confirms no `DbContext`/`UnitOfWork`/`IMapper` in `MoviePresentation`.
**Commit:** `refactor(presentation): MoviesController talks only to IServiceManager`

### Step 19: Repeat the service slice for Reviews and Actors

Apply steps 15–18 to `IReviewService`/`ReviewService` and `IActorService`/`ActorService`, add them to `IServiceManager`, and move their controllers into `MoviePresentation`.

**Verify:** `dotnet run`; all endpoints work end-to-end through the service layer; `MovieApi` now contains no controllers.
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

// MovieCore/Genres.cs  — one source of truth for the well-known name
public static class Genres { public const string Documentary = "Documentary"; }
```

```bash
dotnet ef migrations add GenreManyToMany --project MovieData --startup-project MovieApi
dotnet ef database update --project MovieData --startup-project MovieApi
```

**Verify:** migration applies; a join table (`GenreMovie`) exists; seed includes `Genres.Documentary`.
**Commit:** `feat(core): model Genre as many-to-many`

### Step 21: Require valid genres when creating a movie

A create must reference **≥1** genre and **all** must exist, else `BusinessRuleException` → 400 `ProblemDetails` (ADR 0002). `MovieCreateDto` now carries `GenreIds`.

```csharp
// MovieServices/MovieService.cs   (in CreateAsync, before Add)
if (dto.GenreIds is null || dto.GenreIds.Count == 0)
    throw new BusinessRuleException("A movie needs at least one genre.");

var genres = await uow.Genres.GetByIdsAsync(dto.GenreIds);
if (genres.Count != dto.GenreIds.Distinct().Count())
    throw new BusinessRuleException("One or more genres do not exist.");

movie.Genres = genres;
```

**Verify:** `POST` with `genreIds: []` → 400; with an unknown id → 400; with a real id → 201.
**Commit:** `feat(services): validate genres on movie create`

### Step 22: Add paging with an X-Pagination header

> **New concept: complex query binding.** A query object bound from the query string needs `[FromQuery]` or the binder silently leaves it default.

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
// MoviePresentation/Controllers/MoviesController.cs
[HttpGet]
public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(PaginationParameters paging)  // ← mistake: a complex object from the query string needs [FromQuery] paging
{
    var page = await services.MovieService.GetPageAsync(paging);
    Response.Headers["X-Pagination"] = JsonSerializer.Serialize(page.Meta);
    return Ok(page.Data);
}
```

Add a `PagedResult<T>` (`Data` + `Meta`) in `MovieCore`, and a repository method that does `Skip/Take` + a total count.

**Verify:** `GET /api/movies?pageSize=110&page=2` returns ≤100 items and an `X-Pagination` response header.
**Commit:** `feat(services): page list endpoints with X-Pagination header`

### Step 23: Enforce the structural business rules in the service

Brief **Del 6**, rules 2, 3, 6 — all in the service, all `BusinessRuleException`.

```csharp
// MovieServices — guards before CompleteAsync
if (dto.Budget < 0) throw new BusinessRuleException("Budget may not be negative.");
if (await uow.Movies.TitleExistsAsync(dto.Title)) throw new BusinessRuleException("A movie with that title already exists.");
// when assigning an actor:
if (movie.Actors.Any(a => a.Id == actorId)) throw new BusinessRuleException("Actor already assigned to this movie.");
```

**Verify:** duplicate title → 400; negative budget → 400; re-adding an actor → 400.
**Commit:** `feat(services): enforce budget/title/actor rules`

### Step 24: Enforce the review-count rules

Rules 1 and 4 (sync half): max 10 reviews; max 5 if the movie is older than 20 years.

```csharp
// MovieServices/ReviewService.cs   (in AddReviewAsync)
var movie = await uow.Movies.GetWithReviewsAsync(movieId)
            ?? throw new NotFoundException($"Movie {movieId} not found.");

if (movie.Reviews.Count > 10) throw new BusinessRuleException("A movie may have at most 10 reviews.");  // ← mistake: off-by-one — this allows an 11th; the cap is reached at 10 (use >=)

var ageOver20 = DateTime.UtcNow.Year - movie.Year > 20;
if (ageOver20 && movie.Reviews.Count >= 5)
    throw new BusinessRuleException("A movie older than 20 years may have at most 5 reviews.");
```

**Verify:** adding the 11th review → 400; on a >20-year movie, the 6th → 400.
**Commit:** `feat(services): enforce review-count rules`

### Step 25: Enforce the Documentary caps

Rule 5: if `Documentary` is among a movie's genres, cap actors at 10 and budget at 1,000,000 (ADR 0002).

```csharp
// MovieServices — reusable guard
private static bool IsDocumentary(Movie m) => m.Genres.Any(g => g.Name == Genres.Documentary);

if (IsDocumentary(movie) && movie.Actors.Count > 10)
    throw new BusinessRuleException("A documentary may have at most 10 actors.");
if (IsDocumentary(movie) && movie.Details?.Budget > 1_000_000m)
    throw new BusinessRuleException("A documentary's budget may not exceed 1,000,000.");
```

**Verify:** a Documentary with an 11th actor → 400; budget 1,000,001 → 400; same numbers on a non-Documentary → OK.
**Commit:** `feat(services): enforce Documentary caps`

### Step 26: Add PATCH for Movie + MovieDetails

> **New concept: `JsonPatchDocument`.** A list of `replace`/`add`/`remove` ops. It needs Newtonsoft (the usual reason `[FromBody]` won't bind). We patch one **flat** `MoviePatchDto` spanning both entities.

```bash
dotnet add MovieApi/MovieApi.csproj package Microsoft.AspNetCore.Mvc.NewtonsoftJson --version 10.0.9
```

```csharp
// MovieApi/Program.cs
builder.Services.AddControllers().AddNewtonsoftJson();   // chain onto the existing AddControllers
```

```csharp
// MoviePresentation/Controllers/MoviesController.cs
[HttpPatch("{id:int}")]
public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<MoviePatchDto> patch)
{
    var dto = await services.MovieService.GetPatchModelAsync(id);   // flat: Title, Year, Duration, Synopsis, Language, Budget
    patch.ApplyTo(dto, ModelState);
    if (!TryValidateModel(dto)) return ValidationProblem(ModelState);
    await services.MovieService.ApplyPatchAsync(id, dto);           // re-runs business rules, maps to Movie + Details
    return NoContent();
}
```

**Verify:** `PATCH /api/movies/1` with `[{ "op":"replace","path":"/budget","value":500000 }]` updates only Budget on `MovieDetails`.
**Commit:** `feat(presentation): PATCH movie and details via flat JsonPatchDocument`

### Step 27: Add the test project and mock the data layer

> **New concept: NSubstitute.** Fakes `IUnitOfWork` and its repositories so service logic is tested in isolation — no EF, no database.

```bash
dotnet new xunit -n MovieServices.Tests -f net10.0
dotnet sln add MovieServices.Tests
dotnet add MovieServices.Tests/MovieServices.Tests.csproj reference MovieServices/MovieServices.csproj MovieContracts/MovieContracts.csproj MovieCore/MovieCore.csproj
dotnet add MovieServices.Tests/MovieServices.Tests.csproj package NSubstitute
```

**Verify:** `dotnet test` runs (zero tests yet).
**Commit:** `test(services): add test project with NSubstitute`

### Step 28: Write the four service tests

Cover the grilled set: 11th review rejected, Documentary caps, duplicate title + missing id, happy-path create mapping.

```csharp
// MovieServices.Tests/ReviewServiceTests.cs
[Fact]
public async Task AddReview_WhenMovieHas10Reviews_Throws()
{
    var uow = Substitute.For<IUnitOfWork>();
    uow.Movies.GetWithReviewsAsync(1).Returns(MovieWith(10, reviews: true));
    var sut = new ReviewService(uow, _mapper);

    await Assert.ThrowsAsync<BusinessRuleException>(() => sut.AddReviewAsync(1, new ReviewCreateDto()));
}
```

Add one test each for: Documentary 11th actor/over-budget (+ a non-Documentary control), duplicate title, missing id → `NotFoundException`, and a valid create returning a correctly mapped `MovieDto`.

**Verify:** `dotnet test` — all four pass. (Brief Del 10 wants ≥3.)
**Commit:** `test(services): cover review cap, documentary caps, errors, create mapping`

---

## Phase 2 — Stretch Goals

> Optional/bonus work beyond the brief's mandatory requirements — the review trimmer (Del 6.4 extra) and Del 11 experimentation.

### Step 29: Add CreatedAt to Review

The trimmer removes the *oldest* reviews, so `Review` needs a timestamp (ADR 0004). Builds on Step 24.

```csharp
// MovieCore/Models/Review.cs
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

```bash
dotnet ef migrations add ReviewCreatedAt --project MovieData --startup-project MovieApi
dotnet ef database update --project MovieData --startup-project MovieApi
```

**Verify:** new reviews persist a `CreatedAt`; seed sets sensible values.
**Commit:** `feat(core): timestamp reviews with CreatedAt`

### Step 30: Add the idempotent review-trimmer background service

> **New concept: `BackgroundService` + `IServiceScopeFactory`.** A hosted worker runs on a loop independent of HTTP. It's a **singleton**, so it can't hold a scoped `IUnitOfWork` — it must create a scope per run (ADR 0004). Builds on Steps 24 & 29.

```csharp
// MovieApi/BackgroundServices/ReviewTrimmer.cs
public class ReviewTrimmer(IUnitOfWork uow) : BackgroundService   // ← mistake: a singleton can't capture a scoped IUnitOfWork — inject IServiceScopeFactory and create a scope inside the loop
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // scan ALL movies >20yr with >5 reviews; remove oldest (by CreatedAt) down to 5
            await TrimAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
```

```csharp
// MovieApi/Program.cs
builder.Services.AddHostedService<ReviewTrimmer>();
```

**Verify:** seed a >20-year movie with 8 reviews; within one tick it's trimmed to the 5 newest.
**Commit:** `feat(api): add idempotent review-trimmer background service`

### Step 31: (Optional) Demonstrate the Result<T> alternative on one controller

Del 9 invites showing both error styles. Keep exceptions+`IExceptionHandler` as the house default (ADR 0003); implement one controller (e.g. `ActorsController`) returning a `Result<T>` translated to `ProblemDetails`, purely to demonstrate you understand the trade-off.

**Verify:** that controller returns correct status codes via `Result`; the rest still flow through the handler.
**Commit:** `feat(presentation): demonstrate Result<T> error style on one controller`
```

