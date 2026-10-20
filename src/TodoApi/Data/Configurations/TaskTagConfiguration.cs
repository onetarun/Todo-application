using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Entities;

namespace TodoApi.Data.Configurations;

public class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> builder)
    {
        builder.HasIndex(tt => new { tt.TaskId, tt.TagId }).IsUnique();

        builder.HasOne(tt => tt.Task)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tt => tt.Tag)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
