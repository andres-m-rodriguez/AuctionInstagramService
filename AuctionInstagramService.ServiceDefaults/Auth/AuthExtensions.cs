using AuctionInstagramService.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionInstagramService.ServiceDefaults.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddMockAuth(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddTransient<UserPropagationHandler>();

        services.AddAuthentication(DevAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SchemeName, _ => { });
        services.AddAuthorization();
        return services;
    }
}
