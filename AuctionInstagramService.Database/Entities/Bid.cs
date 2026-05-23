namespace AuctionInstagramService.Database.Entities;

public class Bid
{
    public required Guid Id { get; init; }
    public required Guid AuctionId { get; init; }
    public required string BidderUserId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset PlacedAt { get; init; }

    public Auction? Auction { get; set; }
}
