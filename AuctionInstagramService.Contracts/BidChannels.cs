namespace AuctionInstagramService.Contracts;

public static class BidChannels
{
    public static string For(Guid auctionId) => $"bids:{auctionId}";
}
