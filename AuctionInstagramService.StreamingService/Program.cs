using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using AuctionInstagramService.Contracts;
using AuctionInstagramService.ServiceDefaults.Auth;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");
builder.Services.AddMockAuth();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/auctions/{auctionId:guid}/bids/stream",
    (Guid auctionId, IConnectionMultiplexer redis, CancellationToken ct) =>
        TypedResults.ServerSentEvents(StreamBids(auctionId, redis, ct), eventType: "bid"))
    .RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();

static async IAsyncEnumerable<BidDto> StreamBids(
    Guid auctionId,
    IConnectionMultiplexer redis,
    [EnumeratorCancellation] CancellationToken ct)
{
    var channel = Channel.CreateUnbounded<BidDto>();
    var subscriber = redis.GetSubscriber();
    var redisChannel = RedisChannel.Literal(BidChannels.For(auctionId));

    await subscriber.SubscribeAsync(redisChannel, (_, msg) =>
    {
        if (!msg.HasValue) return;
        var bid = JsonSerializer.Deserialize<BidDto>((string)msg!);
        if (bid is not null) channel.Writer.TryWrite(bid);
    });

    try
    {
        await foreach (var bid in channel.Reader.ReadAllAsync(ct))
            yield return bid;
    }
    finally
    {
        await subscriber.UnsubscribeAsync(redisChannel);
    }
}
