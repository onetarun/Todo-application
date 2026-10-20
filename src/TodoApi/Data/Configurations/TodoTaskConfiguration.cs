using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Entities;

namespace TodoApi.Data.Configurations;

public class TodoTaskConfiguration : IEntityTypeConfiguration<TodoTask>
{
    public void Configure(EntityTypeBuilder<TodoTask> builder)
    {
        builder.ToTable("Tasks", t => t.HasCheckConstraint(
            "CK_Tasks_Priority",
            $"[Priority] BETWEEN {TodoTask.MinPriority} AND {TodoTask.MaxPriority}"));

        builder.Property(t => t.Title).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(t => t.TodoList)
            .WithMany(l => l.Tasks)
            .HasForeignKey(t => t.TodoListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
