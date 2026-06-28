using AutoMapper;                      
using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using MovieCore.Models;
using NSubstitute;

namespace MovieServices.Tests;

public class MovieServiceTests
{
    [Fact]
    public async Task CreateAsync_DuplicateTitle_Throws()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.TitleExistsAsync("Dup", Arg.Any<int?>()).Returns(true);
        var sut = new MovieService(uow, Substitute.For<IMapper>());

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => sut.CreateAsync(new MovieCreateDto { Title = "Dup", GenreIds = [1] }));
    }

    [Fact]
    public async Task GetAsync_MissingId_ThrowsNotFound()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetAsync(404).Returns((Movie?)null);
        var sut = new MovieService(uow, Substitute.For<IMapper>());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetAsync(404));
    }

    [Fact]
    public async Task CreateAsync_Valid_ReturnsMappedDtoAndSaves()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();
        uow.Movies.TitleExistsAsync("New", Arg.Any<int?>()).Returns(false);
        uow.Genres.GetByIdsAsync(Arg.Any<IEnumerable<int>>())
            .Returns([new() { Id = 1, Name = "Drama" }]);

        var entity = new Movie { Title = "New", Year = 2020 };
        mapper.Map<Movie>(Arg.Any<MovieCreateDto>()).Returns(entity);
        mapper.Map<MovieDto>(entity).Returns(new MovieDto { Id = 7, Title = "New" });

        var sut = new MovieService(uow, mapper);

        var result = await sut.CreateAsync(new MovieCreateDto { Title = "New", GenreIds = [1] });

        Assert.Equal(7, result.Id);
        await uow.Received(1).CompleteAsync();
    }
}