namespace MovieData ;

using Microsoft.EntityFrameworkCore;
using MovieCore.Models;

public class MovieContext(DbContextOptions<MovieContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieDetails> MovieDetails => Set<MovieDetails>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    // The skip navigations stay wired throught the explicit join entity,
    // so every existing movie.Actors call site keeps working unchanged.
    modelBuilder.Entity<Movie>()
        .HasMany(m => m.Actors)
        .WithMany(a => a.Movies)
        .UsingEntity<MovieActor>(
            j => j.HasOne(ma => ma.Actor).WithMany().HasForeignKey(ma => ma.ActorId),
            j => j.HasOne(ma => ma.Movie).WithMany(m => m.Cast).HasForeignKey(ma => ma.MovieId));
    }
}
