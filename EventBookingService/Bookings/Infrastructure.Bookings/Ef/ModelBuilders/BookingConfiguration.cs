using Domain.Bookings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Bookings.Ef.ModelBuilders;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder
            .ToTable(TableNames.Bookings)
            .HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.EventId)
            .IsRequired();

        builder.HasIndex(b => b.EventId);

        builder.Property(b => b.Status)
            .IsRequired();

        builder.HasIndex(b => b.Status);

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(b => b.ProcessedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.HasIndex(b => b.UserId);
    }
}
