using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AuctionInstagramService.Database;

public static class DatabaseExtensions
{
    public const string ConnectionName = "auctiondb";

    public static void AddAuctionDatabase(this IHostApplicationBuilder builder)
        => builder.AddNpgsqlDbContext<AuctionDbContext>(
            ConnectionName,
            configureDbContextOptions: opt => opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
}
