using Microsoft.AspNetCore.SignalR;

namespace SinusSynchronousShared.Utils;

public class IdBasedUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext context)
    {
        return context.User!.Claims.SingleOrDefault(c => string.Equals(c.Type, SinusClaimTypes.Uid, StringComparison.Ordinal))?.Value;
    }
}
