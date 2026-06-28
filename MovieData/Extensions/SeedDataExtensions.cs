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
        var drama = new Genre { Name = "Drama" };
        var comedy = new Genre { Name = "Comedy" };
        var documentary = new Genre { Name = Genres.Documentary }; // single source of truth
        var sciFi = new Genre { Name = "Sci-Fi" };
        context.Genres.AddRange(drama, comedy, documentary, sciFi);

// --- Actors (ids 1..14) ---
        var hanks = new Actor { Name = "Tom Hanks", BirthYear = 1956 };
        var robbins = new Actor { Name = "Tim Robbins", BirthYear = 1958 };
        var freeman = new Actor { Name = "Morgan Freeman", BirthYear = 1937 };
        var johansson = new Actor { Name = "Scarlett Johansson", BirthYear = 1984 };
        var murray = new Actor { Name = "Bill Murray", BirthYear = 1950 };
// extra cast (ids 6..14) so the Documentary can hold 10 actors and the cap is testable
        var attenborough = new Actor { Name = "David Attenborough", BirthYear = 1926 };
        var herzog = new Actor { Name = "Werner Herzog", BirthYear = 1942 };
        var weaver = new Actor { Name = "Sigourney Weaver", BirthYear = 1949 };
        var jones = new Actor { Name = "James Earl Jones", BirthYear = 1931 };
        var irons = new Actor { Name = "Jeremy Irons", BirthYear = 1948 };
        var mirren = new Actor { Name = "Helen Mirren", BirthYear = 1945 };
        var neeson = new Actor { Name = "Liam Neeson", BirthYear = 1952 };
        var blanchett = new Actor { Name = "Cate Blanchett", BirthYear = 1969 };
        var elba = new Actor { Name = "Idris Elba", BirthYear = 1972 };
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
                // no reviews — exercises the zero-review case
            },
            new()
            {
                Title = "March of the Penguins", Year = 2005, Duration = 80,
                Genres = { documentary },
                // 10 actors — at the Documentary cap, so an 11th POST → 400
                Actors = { freeman, attenborough, herzog, weaver, jones, irons, mirren, neeson, blanchett, elba },
                Reviews = { new Review { ReviewerName = "Gil", Comment = "Beautiful.", Rating = 4 } }
            },
            new()
            {
                Title = "Her", Year = 2013, Duration = 126,
                Genres = { drama, sciFi },
                Actors = { johansson },
                // 9 reviews — recent movie (2013), so the 10-cap applies; one more POST hits 10, the next 400s
                Reviews =
                {
                    new Review { ReviewerName = "Hana", Comment = "Melancholic.", Rating = 5 },
                    new Review { ReviewerName = "Ivan", Comment = "Thought-provoking.", Rating = 4 },
                    new Review { ReviewerName = "Judy", Comment = "Beautifully shot.", Rating = 5 },
                    new Review { ReviewerName = "Kyle", Comment = "Unsettling and tender.", Rating = 4 },
                    new Review { ReviewerName = "Lena", Comment = "Loved the score.", Rating = 5 },
                    new Review { ReviewerName = "Milo", Comment = "A touch slow.", Rating = 3 },
                    new Review { ReviewerName = "Nora", Comment = "The future feels real.", Rating = 4 },
                    new Review { ReviewerName = "Omar", Comment = "Heartbreaking.", Rating = 5 },
                    new Review { ReviewerName = "Pia", Comment = "Bittersweet.", Rating = 4 }
                }
            }
        };

        context.Movies.AddRange(movies);
        context.SaveChanges();
    }
}