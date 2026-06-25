using Microsoft.EntityFrameworkCore;
using MovieApi.Models;

public class MovieContext(DbContextOptions<MovieContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieDetails> MovieDetails => Set<MovieDetails>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Actor> Actors => Set<Actor>();
}
