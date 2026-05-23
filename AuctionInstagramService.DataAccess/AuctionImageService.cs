using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionInstagramService.DataAccess;

public sealed class AuctionImageService(AuctionDbContext db, BlobServiceClient blobs)
{
    public const string ContainerName = "auction-images";

    public async Task<IReadOnlyList<AuctionImageDto>> ListForAuctionAsync(
        Guid auctionId,
        CancellationToken ct = default
    ) =>
        await db.AuctionImages
            .Where(i => i.AuctionId == auctionId)
            .Select(i => new AuctionImageDto(i.Id, i.AuctionId))
            .ToListAsync(ct);

    public async Task<AuctionImageDto> UploadAsync(
        Guid auctionId,
        Stream content,
        string contentType,
        CancellationToken ct = default
    )
    {
        var container = blobs.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var id = Guid.CreateVersion7();
        var blobName = $"{auctionId}/{id}";

        await container
            .GetBlobClient(blobName)
            .UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        var image = new AuctionImage
        {
            Id = id,
            AuctionId = auctionId,
            BlobName = blobName,
            ContentType = contentType,
        };
        db.AuctionImages.Add(image);
        await db.SaveChangesAsync(ct);

        return new AuctionImageDto(image.Id, image.AuctionId);
    }

    public async Task<(Stream Content, string ContentType)?> DownloadAsync(
        Guid imageId,
        CancellationToken ct = default
    )
    {
        var image = await db.AuctionImages
            .Where(i => i.Id == imageId)
            .Select(i => new { i.BlobName, i.ContentType })
            .SingleOrDefaultAsync(ct);
        if (image is null) return null;

        var blob = blobs.GetBlobContainerClient(ContainerName).GetBlobClient(image.BlobName);
        var resp = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return (resp.Value.Content, image.ContentType);
    }
}
