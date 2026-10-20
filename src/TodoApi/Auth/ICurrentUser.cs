namespace TodoApi.Auth;

public interface ICurrentUser
{
    // Null when the request is anonymous or the token has no valid "sub" claim.
    int? UserId { get; }

    // For endpoints behind the authenticated-user policy, where a missing id indicates a server bug, not a client error.
    int RequiredUserId => UserId ?? throw new InvalidOperationException("No authenticated user id on the current request.");
}
