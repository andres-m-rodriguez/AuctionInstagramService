using AuctionInstagramService.Contracts;

namespace AuctionInstagramService.Database.Entities;

public class Notification
{
    public required Guid Id { get; init; }
    public required string UserId { get; init; }
    public required Guid AuctionId { get; init; }
    public required NotificationType Type { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
