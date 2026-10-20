using System.ComponentModel.DataAnnotations;
using TodoApi.Entities;

namespace TodoApi.Dtos.Tasks;

public record UpdateTaskStatusRequest
{
    [Required, EnumDataType(typeof(TodoTaskStatus))]
    public TodoTaskStatus? Status { get; init; }
}
