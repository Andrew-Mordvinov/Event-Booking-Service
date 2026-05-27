using Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.EF.ModelBuilders;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder
            .ToTable(TableNames.Events)
            .HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .IsRequired();

        builder.HasIndex(e => e.Title);

        builder.Property(e => e.StartAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.StartAt);

        builder.Property(e => e.EndAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.EndAt);

        builder.Property(e => e.TotalSeats)
            .IsRequired();

        builder.Property(e => e.AvailableSeats)
            .IsRequired();
    }
}
