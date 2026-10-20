using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Entities;

namespace TodoApi.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(50);
        // "#RRGGBB"
        builder.Property(t => t.Color).HasMaxLength(7);
    }
}
