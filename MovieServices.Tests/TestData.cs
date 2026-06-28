using MovieCore.Models;

namespace MovieServices.Tests;

internal static class TestData
{
    public static Movie MovieWithReviews(int count, int? year = null)
    {
        var movie = new Movie { Id = 1, Title = "Test Movie", Year = year ?? DateTime.UtcNow.Year };
        for (var i = 0; i < count; i++)
        {
            movie.Reviews.Add(new Review { ReviewerName = $"R{i}", Comment = $"C{i}", Rating = 3 });
        }

        return movie;
    }

    public static Movie MovieWithActors(int count, bool documentary)
    {
        var movie = new Movie { Id = documentary ? 5 : 1, Title = "Test Movie", Year = 2000 };
        if (documentary) movie.Genres.Add(new Genre { Name = Genres.Documentary });
        for (var i = 0; i < count; i++)
        {
            movie.Actors.Add(new Actor { Id = i, Name = $"A{i}" });
        }

        return movie;
    }
}