namespace TodoApi.Entities;

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }

    public List<TaskTag> TaskTags { get; set; } = [];
}
