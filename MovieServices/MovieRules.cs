using MovieCore.Models;

namespace MovieServices;

internal static class MovieRules
{
    public static bool IsDocumentary(Movie movie) => movie.Genres.Any(g => g.Name == Genres.Documentary);
}