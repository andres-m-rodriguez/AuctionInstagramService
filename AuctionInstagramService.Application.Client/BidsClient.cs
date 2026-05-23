using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AuctionInstagramService.Contracts;

namespace AuctionInstagramService.Application.Client;

public sealed class BidsClient(HttpClient http)
{
    public Task<IReadOnlyList<BidDto>?> ListForAuctionAsync(Guid auctionId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<BidDto>>($"api/auctions/{auctionId}/bids", ct);

    public async Task<HttpResponseMessage> PlaceAsync(Guid auctionId, decimal amount, CancellationToken ct = default) =>
        await http.PostAsJsonAsync($"api/auctions/{auctionId}/bids", new PlaceBidRequest(amount), ct);

    public async IAsyncEnumerable<BidDto> StreamAsync(Guid auctionId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var resp = await http.GetAsync(
            $"api/auctions/{auctionId}/bids/stream",
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var parser = SseParser.Create(stream, (_, bytes) =>
            JsonSerializer.Deserialize<BidDto>(bytes, JsonSerializerOptions.Web)!);

        await foreach (var evt in parser.EnumerateAsync(ct))
            yield return evt.Data;
    }
}
