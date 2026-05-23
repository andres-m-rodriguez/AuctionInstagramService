using System.Data;
using System.Text.Json;
using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.Database.Entities;
using AuctionInstagramService.Messaging;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace AuctionInstagramService.DataAccess;

public sealed class BidService(AuctionDbContext db)
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

    public Task<
        OneOf<BidDto, AuctionNotFound, AuctionNotOpen, AuctionEnded, BidTooLow>
    > PlaceAsync(
        Guid auctionId,
        decimal amount,
        string bidderUserId,
        CancellationToken ct = default
    ) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(
            () => PlaceCoreAsync(auctionId, amount, bidderUserId, ct));

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

        var dto = new BidDto(bid.Id, bid.AuctionId, bid.BidderUserId, bid.Amount, bid.PlacedAt);
        db.OutboxEvents.Add(new OutboxEvent
        {
            Id = Guid.CreateVersion7(),
            EventType = nameof(BidMadeEvent),
            Channel = BidChannels.For(auctionId),
            Payload = JsonSerializer.Serialize(new BidMadeEvent(dto)),
            OccurredAt = DateTimeOffset.UtcNow,
            ProcessedAt = null,
        });

        await db.SaveChangesAsync(ct);

        await db.Auctions
            .Where(a => a.Id == auctionId)
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.CurrentHighestBid, amount), ct);

        await tx.CommitAsync(ct);

        return dto;
    }
}
