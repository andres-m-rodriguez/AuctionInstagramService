using System.Security.Claims;
using AuctionInstagramService.Contracts;
using Microsoft.AspNetCore.Http;

namespace AuctionInstagramService.ServiceDefaults.Auth;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public UserContext Get()
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return new UserContext("anonymous", "Anonymous", []);

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var name = user.FindFirstValue(ClaimTypes.Name) ?? id;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return new UserContext(id, name, roles);
    }
}
