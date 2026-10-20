using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Entities;

namespace TodoApi.Data.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasOne(r => r.Task)
            .WithMany(t => t.Reminders)
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
