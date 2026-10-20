using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Lists;

public record CreateTodoListRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }
}
