namespace TodoApi.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    // SHA-256 hex of the token; the raw value is only ever held by the client.
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
