using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuctionInstagramService.Database;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    public AuctionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseNpgsql("Host=localhost;Database=auctiondb;Username=postgres;Password=postgres")
            .Options;
        return new AuctionDbContext(options);
    }
}
