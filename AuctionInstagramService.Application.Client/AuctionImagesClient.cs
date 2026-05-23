using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionInstagramService.Contracts;

namespace AuctionInstagramService.Application.Client;

public sealed class AuctionImagesClient(HttpClient http)
{
    public Task<IReadOnlyList<AuctionImageDto>?> ListForAuctionAsync(Guid auctionId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<AuctionImageDto>>($"api/auctions/{auctionId}/images", ct);

    public async Task<AuctionImageDto> UploadAsync(
        Guid auctionId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var multipart = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(streamContent, "file", fileName);

        var resp = await http.PostAsync($"api/auctions/{auctionId}/images", multipart, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuctionImageDto>(ct))!;
    }

    public string UrlFor(Guid imageId) => $"api/images/{imageId}";
}
