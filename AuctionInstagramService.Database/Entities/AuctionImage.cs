namespace AuctionInstagramService.Database.Entities;

public class AuctionImage
{
    public required Guid Id { get; init; }
    public required Guid AuctionId { get; init; }
    public required string BlobName { get; init; }
    public required string ContentType { get; init; }

    public Auction? Auction { get; set; }
}
