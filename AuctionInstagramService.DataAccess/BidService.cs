using System.Data;
using System.Text.Json;
using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.Database.Entities;
using Microsoft.EntityFrameworkCore;
using OneOf;
using StackExchange.Redis;

namespace AuctionInstagramService.DataAccess;

public sealed class BidService(AuctionDbContext db, IConnectionMultiplexer redis)
{
    public async Task<IReadOnlyList<BidDto>> ListForAuctionAsync(
        Guid auctionId,
        CancellationToken ct = default
    ) =>
        await db.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .Select(b => new BidDto(b.Id, b.AuctionId, b.BidderUserId, b.Amount, b.PlacedAt))
            .ToListAsync(ct);

    public async Task<
        OneOf<BidDto, AuctionNotFound, AuctionNotOpen, AuctionEnded, BidTooLow>
    > PlaceAsync(
        Guid auctionId,
        decimal amount,
        string bidderUserId,
        CancellationToken ct = default
    )
    {
        var result = await db.Database.CreateExecutionStrategy().ExecuteAsync(
            () => PlaceCoreAsync(auctionId, amount, bidderUserId, ct));

        if (result.IsT0)
        {
            await redis.GetSubscriber().PublishAsync(
                RedisChannel.Literal(BidChannels.For(auctionId)),
                JsonSerializer.Serialize(result.AsT0));
        }

        return result;
    }

    private async Task<
        OneOf<BidDto, AuctionNotFound, AuctionNotOpen, AuctionEnded, BidTooLow>
    > PlaceCoreAsync(
        Guid auctionId,
        decimal amount,
        string bidderUserId,
        CancellationToken ct
    )
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var snapshot = await db.Auctions
            .Where(a => a.Id == auctionId)
            .Select(a => new
            {
                a.StartingPrice,
                a.Status,
                a.EndsAt,
                a.CurrentHighestBid,
            })
            .SingleOrDefaultAsync(ct);

        if (snapshot is null)
            return new AuctionNotFound();
        if (snapshot.Status != AuctionStatus.Open)
            return new AuctionNotOpen();
        if (DateTimeOffset.UtcNow > snapshot.EndsAt)
            return new AuctionEnded();

        var minimum = snapshot.CurrentHighestBid ?? snapshot.StartingPrice;
        if (amount <= minimum)
            return new BidTooLow();

        var bid = new Bid
        {
            Id = Guid.CreateVersion7(),
            AuctionId = auctionId,
            BidderUserId = bidderUserId,
            Amount = amount,
            PlacedAt = DateTimeOffset.UtcNow,
        };
        db.Bids.Add(bid);
        await db.SaveChangesAsync(ct);

        await db.Auctions
            .Where(a => a.Id == auctionId)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.CurrentHighestBid, amount), ct);

        await tx.CommitAsync(ct);

        return new BidDto(bid.Id, bid.AuctionId, bid.BidderUserId, bid.Amount, bid.PlacedAt);
    }
}
