using Infrastructure.Bookings.Ef.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Bookings.Ef.ModelBuilders;

public class BookingConfirmedOutboxItemConfiguration : IEntityTypeConfiguration<BookingConfirmedOutboxItem>
{
    public void Configure(EntityTypeBuilder<BookingConfirmedOutboxItem> builder)
    {
        builder
            .ToTable(TableNames.BookingConfirmedOutbox)
            .HasKey(b => new { b.BookingId, b.EventId });

        builder.HasIndex(b => b.EventId);

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.Property(b => b.Seats)
            .IsRequired();

        builder.Property(b => b.Approved)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
    }
}
