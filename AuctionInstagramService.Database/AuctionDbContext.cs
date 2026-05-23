using AuctionInstagramService.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionInstagramService.Database;

public class AuctionDbContext(DbContextOptions<AuctionDbContext> options) : DbContext(options)
{
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<AuctionImage> AuctionImages => Set<AuctionImage>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
}
