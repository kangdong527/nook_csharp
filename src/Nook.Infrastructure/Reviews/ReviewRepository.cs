using Microsoft.EntityFrameworkCore;
using Nook.Application.Reviews;
using Nook.Domain.Places;
using Nook.Domain.Reviews;
using Nook.Domain.Users;
using Nook.Infrastructure.Persistence;

namespace Nook.Infrastructure.Reviews;

internal sealed class ReviewRepository(NookDbContext db) : IReviewRepository
{
    public Task<bool> ExistsAsync(UserId userId, PlaceId placeId, CancellationToken ct) =>
        db.Reviews.AnyAsync(r => r.UserId == userId && r.PlaceId == placeId, ct);

    public Task<bool> ExistsAsync(ReviewId reviewId, CancellationToken ct) =>
        db.Reviews.AnyAsync(r => r.Id == reviewId, ct);
}
