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

        var hanks = new Actor { Name = "Tom Hanks", BirthYear = 1956 };
        var robbins = new Actor { Name = "Tim Robbins", BirthYear = 1958 };

        var drama = new Genre { Name = "Drama" };
        var comedy = new Genre { Name = "Comedy" };
        var action = new Genre { Name = "Action" };
        var horror = new Genre { Name = "Horror" };
        var romance = new Genre { Name = "Romance" };
        var sciFi = new Genre { Name = "Sci-Fi" };
        var thriller = new Genre { Name = "Thriller" };
        var western = new Genre { Name = "Western" };
        var documentary = new Genre { Name = "Documentary" };
        var animation = new Genre { Name = "Animation" };
        var family = new Genre { Name = "Family" };
        var fantasy = new Genre { Name = "Fantasy" };
        var musical = new Genre { Name = "Musical" };
        var mystery = new Genre { Name = "Mystery" };
        var war = new Genre { Name = "War" };
        var crime = new Genre { Name = "Crime" };
        var adventure = new Genre { Name = "Adventure" };
        var history = new Genre { Name = "History" };
        var reality = new Genre { Name = "Reality" };
        context.Genres.AddRange(drama, comedy, action, horror, romance, sciFi, thriller, western, documentary, animation, family, fantasy, musical, mystery, war, crime, adventure, history, reality);
        // The whole graph as plain objects - EF figures out how to join them
        var movie = new Movie
        {
            Title = "Forrest Gump",
            Year = 1994,
            Genres = { drama },
            Duration = 142,
            Details = new MovieDetails
            {
                Synopsis = "Life is like a box of chocolates",
                Language = "English",
                Budget = 55_000_000m
            },
            Reviews =
            {
                new Review { ReviewerName = "Alice", Comment = "Classic!", Rating = 5 },
                new Review { ReviewerName = "Bob", Comment = "Touching.", Rating = 4 }
            },
            Actors = { hanks }
        };

        context.Movies.Add(movie);
        context.Actors.Add(robbins);
        context.SaveChanges();
    }
}
