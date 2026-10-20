using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Entities;

namespace TodoApi.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.Property(n => n.Content).HasMaxLength(4000);

        builder.HasOne(n => n.Task)
            .WithMany(t => t.Notes)
            .HasForeignKey(n => n.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
