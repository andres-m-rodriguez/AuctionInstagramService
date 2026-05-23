using System.Data;
using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuctionInstagramService.DataAccess;

public sealed class AuctionClosingService(
    IServiceScopeFactory scopeFactory,
    ILogger<AuctionClosingService> logger
) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auction closing sweep failed");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task CloseExpiredAsync(CancellationToken ct)
    {
        List<Guid> expiredIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
            var now = DateTimeOffset.UtcNow;
            expiredIds = await db.Auctions
                .Where(a => a.Status == AuctionStatus.Open && a.EndsAt <= now)
                .Select(a => a.Id)
                .ToListAsync(ct);
        }

        foreach (var auctionId in expiredIds)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
                await db.Database.CreateExecutionStrategy().ExecuteAsync(
                    () => CloseOneAsync(db, auctionId, ct));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close auction {AuctionId}", auctionId);
            }
        }
    }

    private async Task CloseOneAsync(AuctionDbContext db, Guid auctionId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var auction = await db.Auctions
            .Where(a => a.Id == auctionId)
            .Select(a => new { a.Id, a.Title, a.Status, a.EndsAt, a.CurrentHighestBid })
            .SingleOrDefaultAsync(ct);

        if (auction is null || auction.Status != AuctionStatus.Open) return;

        var bidders = await db.Bids
            .Where(b => b.AuctionId == auctionId)
            .GroupBy(b => b.BidderUserId)
            .Select(g => new { UserId = g.Key, MaxBid = g.Max(b => b.Amount) })
            .ToListAsync(ct);

        await db.Auctions
            .Where(a => a.Id == auctionId)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.Status, AuctionStatus.Closed), ct);

        var winnerUserId = bidders.Count == 0
            ? null
            : bidders.OrderByDescending(b => b.MaxBid).First().UserId;

        if (winnerUserId is not null && auction.CurrentHighestBid is decimal price)
        {
            var now = DateTimeOffset.UtcNow;
            var notifications = new List<Notification>
            {
                new()
                {
                    Id = Guid.CreateVersion7(),
                    UserId = winnerUserId,
                    AuctionId = auctionId,
                    Type = NotificationType.Won,
                    Message = $"You won \"{auction.Title}\" for {price:C}!",
                    CreatedAt = now,
                },
            };

            foreach (var bidder in bidders.Where(b => b.UserId != winnerUserId))
            {
                notifications.Add(new Notification
                {
                    Id = Guid.CreateVersion7(),
                    UserId = bidder.UserId,
                    AuctionId = auctionId,
                    Type = NotificationType.Lost,
                    Message = $"\"{auction.Title}\" sold for {price:C}. Sorry, you didn't get it.",
                    CreatedAt = now,
                });
            }

            db.Notifications.AddRange(notifications);
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);

        logger.LogInformation(
            "Closed auction {AuctionId} (\"{Title}\"); winner={Winner}, losers notified={LoserCount}",
            auctionId, auction.Title, winnerUserId ?? "(none)", Math.Max(0, bidders.Count - 1));
    }
}
