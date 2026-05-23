using Microsoft.Extensions.DependencyInjection;

namespace AuctionInstagramService.Application.Client;

public static class ApplicationClientExtensions
{
    public static IServiceCollection AddAuctionAppClients(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient<AuctionsClient>(c => c.BaseAddress = baseAddress);
        services.AddHttpClient<AuctionImagesClient>(c => c.BaseAddress = baseAddress);
        services.AddHttpClient<BidsClient>(c => c.BaseAddress = baseAddress);
        return services;
    }
}
