using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Entities;

namespace TodoApi.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Name).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
