using AuctionInstagramService.Contracts;

namespace AuctionInstagramService.ServiceDefaults.Auth;

public sealed class UserPropagationHandler(ICurrentUser currentUser) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = currentUser.Get();
        if (user.UserId != "anonymous")
            request.Headers.TryAddWithoutValidation(DevAuthHandler.UserHeader, user.UserId);
        return base.SendAsync(request, cancellationToken);
    }
}
