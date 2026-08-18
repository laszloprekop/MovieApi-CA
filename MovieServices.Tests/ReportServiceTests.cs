using MovieCore.DomainContracts;
using MovieCore.Models;
using NSubstitute;

namespace MovieServices.Tests;

public class ReportServiceTests
{
    private static Movie MovieRated(int id, string title, string genre, params int[] ratings)
    {
        var movie = new Movie { Id = id, Title = title, Genres = { new Genre { Name = genre } } };
        foreach (var rating in ratings)
            movie.Reviews.Add(new Review { ReviewerName = "R", Comment = "C", Rating = rating });
        return movie;
    }

    [Fact]
    public async Task GetDashboardAsync_NoReviews_AverageIsNullNotZero()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetAllForReportsAsync().Returns([MovieRated(1, "Silent", "Drama")]);
        var sut = new ReportService(uow);

        var dashboard = await sut.GetDashboardAsync();

        Assert.Null(dashboard.AverageRating);
        Assert.Equal(0, dashboard.ReviewCount);
    }

    [Fact]
    public async Task GetDashboardAsync_TopListIsCappedAtFivePerGenre()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetAllForReportsAsync().Returns(
            Enumerable.Range(1, 7).Select(i => MovieRated(i, $"M{i}", "Drama", i % 5 + 1)).ToArray());
        var sut = new ReportService(uow);

        var dashboard = await sut.GetDashboardAsync();

        var drama = Assert.Single(dashboard.TopRatedPerGenre);
        Assert.Equal(5, drama.Movies.Count);
        Assert.Equal(drama.Movies.OrderByDescending(m => m.AverageRating), drama.Movies);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsActorsAcrossMovies()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var shared = new Actor { Id = 9, Name = "Busy" };
        var m1 = MovieRated(1, "A", "Drama");
        var m2 = MovieRated(2, "B", "Comedy");
        m1.Actors.Add(shared);
        m2.Actors.Add(shared);
        m2.Actors.Add(new Actor { Id = 2, Name = "Once" });
        uow.Movies.GetAllForReportsAsync().Returns([m1, m2]);
        var sut = new ReportService(uow);

        var dashboard = await sut.GetDashboardAsync();

        Assert.Equal(9, dashboard.MostActiveActors[0].Id);
        Assert.Equal(2, dashboard.MostActiveActors[0].MovieCount);
    }
}
