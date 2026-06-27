using Microsoft.EntityFrameworkCore;
using MovieCore.DomainContracts;
using MovieCore.Models;

namespace MovieData.Repositories;

public class GenreRepository(MovieContext context) : IGenreRepository
{
    public async Task<List<Genre>> GetByIdsAsync(IEnumerable<int> ids) =>
        await context.Genres.Where(g => ids.Contains(g.Id)).ToListAsync();
}