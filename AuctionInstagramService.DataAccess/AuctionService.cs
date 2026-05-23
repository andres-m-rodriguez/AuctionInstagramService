using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionInstagramService.DataAccess;

public sealed class AuctionService(AuctionDbContext db)
{
    public async Task<IReadOnlyList<AuctionDto>> ListAsync(CancellationToken ct = default) =>
        await db.Auctions
            .Select(a => new AuctionDto(
                a.Id,
                a.Title,
                a.Description,
                a.StartingPrice,
                a.StartsAt,
                a.EndsAt,
                a.CreatedByUserId,
                a.Status,
                a.CurrentHighestBid))
            .ToListAsync(ct);

    public Task<AuctionDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Auctions
            .Where(a => a.Id == id)
            .Select(a => new AuctionDto(
                a.Id,
                a.Title,
                a.Description,
                a.StartingPrice,
                a.StartsAt,
                a.EndsAt,
                a.CreatedByUserId,
                a.Status,
                a.CurrentHighestBid))
            .SingleOrDefaultAsync(ct);

    public async Task<AuctionDto> CreateAsync(AuctionDto dto, CancellationToken ct = default)
    {
        var entity = new Auction
        {
            Id = Guid.CreateVersion7(),
            Title = dto.Title,
            Description = dto.Description,
            StartingPrice = dto.StartingPrice,
            StartsAt = dto.StartsAt.ToUniversalTime(),
            EndsAt = dto.EndsAt.ToUniversalTime(),
            CreatedByUserId = dto.CreatedByUserId,
            Status = dto.Status,
            CurrentHighestBid = null,
        };
        db.Auctions.Add(entity);
        await db.SaveChangesAsync(ct);
        return dto with { Id = entity.Id, CurrentHighestBid = null };
    }
}
