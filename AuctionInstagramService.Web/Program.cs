using System.Security.Claims;
using AuctionInstagramService.ServiceDefaults.Auth;
using AuctionInstagramService.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddCookieAuth();
builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOutputCache();

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        routes:
        [
            new RouteConfig
            {
                RouteId = "bids-stream",
                ClusterId = "streamingservice",
                Order = 1,
                Match = new RouteMatch { Path = "/api/auctions/{auctionId}/bids/stream" },
                Transforms =
                [
                    new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" },
                ],
            },
            new RouteConfig
            {
                RouteId = "api",
                ClusterId = "apiservice",
                Order = 2,
                Match = new RouteMatch { Path = "/api/{**catch-all}" },
                Transforms =
                [
                    new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" },
                ],
            },
        ],
        clusters:
        [
            new ClusterConfig
            {
                ClusterId = "apiservice",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["d1"] = new() { Address = "http://apiservice" },
                },
            },
            new ClusterConfig
            {
                ClusterId = "streamingservice",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["d1"] = new() { Address = "http://streamingservice" },
                },
            },
        ])
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(ctx => ctx.AddRequestTransform(t =>
    {
        var userId = t.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            t.ProxyRequest.Headers.TryAddWithoutValidation(DevAuthHandler.UserHeader, userId);
        return ValueTask.CompletedTask;
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapPost("/auth/login", async (HttpContext ctx, [FromForm] string username, [FromForm] string? returnUrl) =>
{
    if (string.IsNullOrWhiteSpace(username))
        return Results.Redirect("/login");

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, username),
        new Claim(ClaimTypes.Name, username),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthExtensions.SchemeName);
    await ctx.SignInAsync(CookieAuthExtensions.SchemeName, new ClaimsPrincipal(identity));

    // Only allow same-site relative paths back; reject anything that could be an open redirect.
    var target = !string.IsNullOrEmpty(returnUrl)
        && !returnUrl.StartsWith("//")
        && !returnUrl.Contains(':')
        ? "/" + returnUrl.TrimStart('/')
        : "/";
    return Results.Redirect(target);
});

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthExtensions.SchemeName);
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AuctionInstagramService.Web.Client._Imports).Assembly);

app.MapReverseProxy().RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();
