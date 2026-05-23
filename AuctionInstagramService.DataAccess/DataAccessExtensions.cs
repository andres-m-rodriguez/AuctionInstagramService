using Microsoft.Extensions.DependencyInjection;

namespace AuctionInstagramService.DataAccess;

public static class DataAccessExtensions
{
    public static IServiceCollection AddAuctionDataAccess(this IServiceCollection services)
    {
        services.AddScoped<AuctionService>();
        services.AddScoped<AuctionImageService>();
        services.AddScoped<BidService>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<AuctionClosingService>();
        return services;
    }
}
