using MovieCore.DomainContracts;
using MovieCore.DTOs;
using MovieCore.Exceptions;
using NSubstitute;

namespace MovieServices.Tests;

public class ReviewServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenMovieAlreadyHas10Reviews_Throws()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Movies.GetWithReviewsAsync(1).Returns(TestData.MovieWithReviews(10));
        var sut = new ReviewService(uow);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CreateAsync(1, new ReviewDto { ReviewerName = "X", Comment = "c", Rating = 4 }));
    }
}