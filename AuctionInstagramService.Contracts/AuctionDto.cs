namespace AuctionInstagramService.Contracts;

public record AuctionDto(
    Guid Id,
    string Title,
    string Description,
    decimal StartingPrice,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string CreatedByUserId,
    AuctionStatus Status,
    decimal? CurrentHighestBid);
