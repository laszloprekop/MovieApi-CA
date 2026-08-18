using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Models;

namespace MovieServices;

public class ReportService(IUnitOfWork uow) : IReportService
{
    private const int TopCount = 5;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var movies = (await uow.Movies.GetAllForReportsAsync()).ToList();

        var allRatings = movies.SelectMany(m => m.Reviews).Select(r => r.Rating).ToList();

        // A movie carrying several genres counts in each — the pairing
        // flattens N:M into (genre, movie) rows before grouping.
        var topRatedPerGenre = movies
            .SelectMany(m => m.Genres.Select(g => (Genre: g.Name, Movie: m)))
            .GroupBy(pair => pair.Genre)
            .OrderBy(group => group.Key)
            .Select(group => new GenreTopMoviesDto(
                group.Key,
                group.Select(pair => ToRating(pair.Movie))
                    .OrderByDescending(m => m.AverageRating)
                    .ThenByDescending(m => m.ReviewCount)
                    .Take(TopCount)
                    .ToList()))
            .ToList();

        var mostActiveActors = movies
            .SelectMany(m => m.Actors)
            .GroupBy(a => a.Id)
            .Select(group => new ActorActivityDto(group.Key, group.First().Name, group.Count()))
            .OrderByDescending(a => a.MovieCount)
            .ThenBy(a => a.Name)
            .Take(TopCount)
            .ToList();

        return new DashboardDto(
            // No reviews means no average — null, not a misleading 0.
            allRatings.Count > 0 ? Math.Round(allRatings.Average(), 2) : null,
            allRatings.Count,
            topRatedPerGenre,
            mostActiveActors);
    }

    private static MovieRatingDto ToRating(Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.Reviews.Count > 0 ? Math.Round(movie.Reviews.Average(r => r.Rating), 2) : 0,
        movie.Reviews.Count);
}
