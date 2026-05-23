using AuctionInstagramService.Contracts;

namespace AuctionInstagramService.Database.Entities;

public class Auction
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required decimal StartingPrice { get; init; }
    public required DateTimeOffset StartsAt { get; init; }
    public required DateTimeOffset EndsAt { get; init; }
    public required string CreatedByUserId { get; init; }
    public required AuctionStatus Status { get; init; }
    public required decimal? CurrentHighestBid { get; init; }

    public ICollection<AuctionImage> Images { get; set; } = [];
    public ICollection<Bid> Bids { get; set; } = [];
}
