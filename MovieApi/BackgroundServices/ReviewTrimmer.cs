using MovieCore.DomainContracts;

namespace MovieApi.BackgroundServices;

public class ReviewTrimmer(IServiceScopeFactory scopeFactory, ILogger<ReviewTrimmer> logger) : BackgroundService
{
    private const int KeepNewest = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TrimAsync();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error trimming reviews");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task TrimAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoffYear = DateTime.UtcNow.Year - 20; // older than 20 years

        var movies = await uow.Movies.GetAllWithReviewsAsync();

        foreach (var movie in movies)
        {
            if (movie.Year >= cutoffYear || movie.Reviews.Count <= KeepNewest)
                continue; // idempotency, skip recent or already within range

            var toRemove = movie.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Skip(KeepNewest)
                .ToList();

            foreach (var review in toRemove)
                uow.Reviews.Remove(review);

            logger.LogInformation("Trimmed {Count} old review(s) for movie [{MovieId}] {Title}"
                , toRemove.Count, movie.Title, movie.Id);
        }

        await uow.CompleteAsync();
    }
}