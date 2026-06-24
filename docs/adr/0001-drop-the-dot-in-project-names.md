# Drop the dot in project names

The central domain entity is `Movie`. The conventional Clean Architecture project names (`Movie.Core`, `Movie.Data`, …) introduce a namespace segment literally named `Movie`, which collides with the `Movie` type and makes references like `Movie.Models.Actor` ambiguous in C#.

We resolve this by dropping the dot — projects are `MovieApi`, `MovieCore`, `MovieData`, `MovieServices`, `MovieContracts`, `MoviePresentation` — rather than renaming the entity to `VideoMovie`.

**Why:** `Movie` is the ubiquitous-language term; it should not be distorted across every DTO, service, URL noun, and mapper to satisfy a tooling constraint. The cost is cosmetic (dotless project names look slightly less idiomatic than dotted ones); the benefit is the domain vocabulary stays clean.
