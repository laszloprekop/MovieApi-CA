namespace MovieCore.DTOs
{
    // The dashboard payload — one GET, everything the admin view renders.
    // Records: pure aggregate output, no identity, no mutation.
    public record MovieRatingDto(int Id, string Title, double AverageRating, int ReviewCount);

    public record GenreTopMoviesDto(string Genre, List<MovieRatingDto> Movies);

    public record ActorActivityDto(int Id, string Name, int MovieCount);

    public record DashboardDto(
        double? AverageRating,
        int ReviewCount,
        List<GenreTopMoviesDto> TopRatedPerGenre,
        List<ActorActivityDto> MostActiveActors);
}
