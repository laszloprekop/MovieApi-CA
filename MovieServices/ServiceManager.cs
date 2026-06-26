using AutoMapper;
using MovieContracts;
using MovieCore.DomainContracts;

namespace MovieServices;

public class ServiceManager(IUnitOfWork uow, IMapper mapper) : IServiceManager
{
    private readonly Lazy<IMovieService> _movie = new(() => new MovieService(uow, mapper));
    private readonly Lazy<IReviewService> _review = new(() => new ReviewService(uow));
    private readonly Lazy<IActorService> _actor = new(() => new ActorService(uow));
    
    public IMovieService MovieService => _movie.Value;
    public IReviewService ReviewService => _review.Value;
    public IActorService ActorService => _actor.Value;
}