using AuctionInstagramService.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionInstagramService.ServiceDefaults.Auth;

public static class CookieAuthExtensions
{
    public const string SchemeName = CookieAuthenticationDefaults.AuthenticationScheme;

    public static IServiceCollection AddCookieAuth(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(SchemeName)
            .AddCookie(SchemeName, opt =>
            {
                opt.Cookie.Name = "auction.auth";
                opt.LoginPath = "/login";
                opt.LogoutPath = "/auth/logout";
                opt.ExpireTimeSpan = TimeSpan.FromDays(7);
                opt.SlidingExpiration = true;
            });
        services.AddAuthorization();
        return services;
    }
}
