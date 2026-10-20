namespace TodoApi.Dtos.Tags;

public record TagDto(int Id, string Name, string Color)
{
    // "#RRGGBB", matching the 7-character Color column.
    public const string ColorPattern = "^#[0-9A-Fa-f]{6}$";
}
