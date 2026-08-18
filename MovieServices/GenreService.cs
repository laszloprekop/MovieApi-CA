using MovieContracts;
using MovieCore.DomainContracts;
using MovieCore.DTOs;

namespace MovieServices;

public class GenreService(IUnitOfWork uow) : IGenreService
{
    public async Task<IEnumerable<GenreDto>> GetAllAsync() =>
        (await uow.Genres.GetAllAsync())
        .Select(g => new GenreDto { Id = g.Id, Name = g.Name });
}
