namespace TodoApi.Entities;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<TodoList> TodoLists { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
