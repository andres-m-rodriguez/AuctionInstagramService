namespace AuctionInstagramService.Messaging;

public static class BidChannels
{
    public static string For(Guid auctionId) => $"bids:{auctionId}";
}
