# Review trimmer is an idempotent background service

Rule 4 ("a movie over 20 years old may have at most 5 reviews") cannot be fully enforced by a
synchronous guard on adding a review: a movie can age past 20 while already holding 6-10 reviews. We
enforce the invariant with both an add-time guard **and** a `BackgroundService` running every 10
minutes.

Decisions:
- **`Review` gains a `CreatedAt`** so "oldest" is well-defined; the trimmer removes by smallest
  `CreatedAt`. (Requires a migration + seed values.)
- **Each run is idempotent and stateless**: it scans *all* movies older than 20 with more than 5
  reviews and trims each down to 5 — rather than tracking which movies "just" crossed 20. A missed
  run self-heals on the next tick. This deviates from the brief's "films that just turned 20"
  wording in favour of robustness.
- **The trimmer creates its own DI scope** (`IServiceScopeFactory`) per run. A `BackgroundService`
  is a singleton; `IUnitOfWork`/`DbContext` are scoped. Resolving them directly would be a
  captive-dependency bug. Do not "simplify" this by injecting `IUnitOfWork` into the constructor.
