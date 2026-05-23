namespace AuctionInstagramService.Contracts;

public record BidDto(Guid Id, Guid AuctionId, string BidderUserId, decimal Amount, DateTimeOffset PlacedAt);
