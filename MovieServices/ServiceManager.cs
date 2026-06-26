using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;

namespace MovieServices;

public class ServiceManager(IUnitOfWork uow, IMapper mapper) : IServiceManager
{
    private readonly Lazy<IMovieService> _movie = new(() => new MovieService(uow, mapper));
    public IMovieService MovieService => _movie.Value;
}