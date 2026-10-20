using Microsoft.IdentityModel.JsonWebTokens;

namespace TodoApi.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int? UserId =>
        int.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : null;
}
