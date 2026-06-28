using MovieCore.DomainContracts;
using MovieCore.Exceptions;
using MovieCore.Models;
using NSubstitute;

namespace MovieServices.Tests;

public class ActorServiceTests
{
    [Fact]
    public async Task AddToMovieAsync_DocumentaryAt10Actors_Throws()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithActorsAsync(5).Returns(TestData.MovieWithActors(10, documentary: true));
        uow.Actors.GetAsync(99).Returns(new Actor { Id = 99, Name = "New" });
        var sut = new ActorService(uow);

        await Assert.ThrowsAsync<BusinessRuleException>(() => sut.AddToMovieAsync(5, 99));
    }

    [Fact]
    public async Task AddToMovieAsync_NonDocumentaryAt10Actors_Saves()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithActorsAsync(1).Returns(TestData.MovieWithActors(10, documentary: false));
        uow.Actors.GetAsync(99).Returns(new Actor { Id = 99, Name = "New" });
        var sut = new ActorService(uow);

        await sut.AddToMovieAsync(1, 99); // no exception — cap doesn't apply
        await uow.Received(1).CompleteAsync(); // and it persisted
    }
}