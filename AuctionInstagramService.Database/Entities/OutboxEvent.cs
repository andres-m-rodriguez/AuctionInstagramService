namespace AuctionInstagramService.Database.Entities;

public class OutboxEvent
{
    public required Guid Id { get; init; }
    public required string EventType { get; init; }
    public required Guid AggregateId { get; init; }
    public required string Payload { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
