# MovieAPI

A Web API for a film catalogue, refactored from a single project into a Clean Architecture
solution. This glossary fixes the domain vocabulary; it is not a spec.

## Catalogue

**Movie**:
A film in the catalogue. Carries its own core attributes (Title, Year, Duration) and is the
principal of a one-to-one with MovieDetails. Has many Genres, many Reviews, and many Actors.
_Avoid_: Film, VideoMovie.

**Title**:
A Movie's name. The canonical term whenever referring to what a movie is called; rule 6 ("no two
movies share the same name") is a uniqueness constraint on Title.
_Avoid_: Name (for movies).

**MovieDetails**:
The dependent half of a Movie's one-to-one — the heavier, optional attributes: Synopsis, Language,
and Budget. Lives separately from Movie but is conceptually part of the same film.

**Budget**:
The production cost of a Movie, held on MovieDetails. Must never be negative.

**Genre**:
A category a Movie belongs to (e.g. Action, Documentary). A Movie has many Genres and a Genre has
many Movies (many-to-many). Identified by a unique Name.

**Documentary**:
A specific, well-known Genre with extra rules (capped actor count and budget). A Movie is a
Documentary when "Documentary" is among its Genres. The name lives once as a domain constant shared
by the seeder and the rules.

**Review**:
A viewer's rating (1-5) and comment on one Movie, with a creation time. A Movie has many Reviews,
capped by business rules; "oldest" Review means the earliest creation time.

**Actor**:
A person who appears in Movies. An Actor is in many Movies and a Movie has many Actors
(many-to-many); the same Actor may not be assigned to the same Movie twice.
