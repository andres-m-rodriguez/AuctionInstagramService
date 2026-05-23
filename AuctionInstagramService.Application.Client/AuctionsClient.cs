using System.Net.Http.Json;
using AuctionInstagramService.Contracts;

namespace AuctionInstagramService.Application.Client;

public sealed class AuctionsClient(HttpClient http)
{
    public Task<IReadOnlyList<AuctionDto>?> ListAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<AuctionDto>>("api/auctions", ct);

    public Task<AuctionDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AuctionDto>($"api/auctions/{id}", ct);

    public async Task<AuctionDto> CreateAsync(AuctionDto dto, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/auctions", dto, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuctionDto>(ct))!;
    }
}
