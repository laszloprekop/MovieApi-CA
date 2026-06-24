# Genres are many-to-many

The brief (Del 5) models Genre as one-to-many: one Movie has exactly one Genre. We instead model
it as **many-to-many** — a Movie has a set of Genres, a Genre has many Movies — because real film
catalogues are genuinely multi-genre, and the lecture itself flags 1:M as a simplification.

**Consequences we committed to:**
- **Documentary cap (rule 5)** applies if `"Documentary"` is *among* a movie's genres (`.Any`
  semantics), not only when it is the sole genre. A documentary is a documentary even if also tagged
  Action.
- **On create/update**, a Movie must reference **at least one** Genre, and **every** referenced
  Genre must already exist. A missing genre returns `ProblemDetails`; genres are never created as a
  side effect of creating a Movie.

**Why record it:** this diverges from a graded brief and reshapes migrations, seed, DTOs, and the
rule-5 check. Without this note a reader would assume the 1:M deviation was a mistake.
