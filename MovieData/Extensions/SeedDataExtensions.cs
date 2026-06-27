using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieCore.Models;

namespace MovieData.Extensions;

public static class SeedDataExtensions
{
    public static void SeedData(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieContext>();
        context.Database.Migrate();
        if (context.Movies.Any()) return; // already seeded, idempotency guard

        // --- Genres (ids 1..4 on a fresh DB) ---
        var drama = new Genre { Name = Genres.Drama };    // single source of truth
        var comedy = new Genre { Name = "Comedy" };
        var documentary = new Genre { Name = "Documentary" };
        var sciFi = new Genre { Name = "Sci-Fi" };
        context.Genres.AddRange(drama, comedy, documentary, sciFi);

// --- Actors (ids 1..5) ---
        var hanks = new Actor { Name = "Tom Hanks", BirthYear = 1956 };
        var robbins = new Actor { Name = "Tim Robbins", BirthYear = 1958 };
        var freeman = new Actor { Name = "Morgan Freeman", BirthYear = 1937 };
        var johansson = new Actor { Name = "Scarlett Johansson", BirthYear = 1984 };
        var murray = new Actor { Name = "Bill Murray", BirthYear = 1950 };
        context.Actors.AddRange(hanks, robbins, freeman, johansson, murray);

// --- Movies (ids 1..6) — varied genres, shared actors, uneven review counts ---
        var movies = new List<Movie>
        {
            new()
            {
                Title = "Forrest Gump", Year = 1994, Duration = 142,
                Genres = { drama },
                Actors = { hanks },
                Details = new MovieDetails
                    { Synopsis = "Life is like a box of chocolates", Language = "English", Budget = 55_000_000m },
                Reviews =
                {
                    new Review { ReviewerName = "Alice", Comment = "Classic!", Rating = 5 },
                    new Review { ReviewerName = "Bob", Comment = "Touching.", Rating = 4 }
                }
            },
            new()
            {
                Title = "The Shawshank Redemption", Year = 1994, Duration = 142,
                Genres = { drama },
                Actors = { robbins, freeman },
                Reviews =
                {
                    new Review { ReviewerName = "Cara", Comment = "Masterpiece.", Rating = 5 },
                    new Review { ReviewerName = "Dan", Comment = "Hopeful.", Rating = 5 },
                    new Review { ReviewerName = "Eve", Comment = "Slow start.", Rating = 3 }
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
                Actors = { freeman }, // narrator
                Reviews = { new Review { ReviewerName = "Gil", Comment = "Beautiful.", Rating = 4 } }
            },
            new()
            {
                Title = "Her", Year = 2013, Duration = 126,
                Genres = { drama, sciFi },
                Actors = { johansson },
                Reviews =
                {
                    new Review { ReviewerName = "Hana", Comment = "Melancholic.", Rating = 5 },
                    new Review { ReviewerName = "Ivan", Comment = "Thought-provoking.", Rating = 4 }
                }
            }
        };

        context.Movies.AddRange(movies);
        context.SaveChanges();
    }
}