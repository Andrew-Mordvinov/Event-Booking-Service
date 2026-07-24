using Infrastructure.Events.Ef.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Events.Ef.ModelBuilders;

public class BookingConfirmedInboxItemConfiguration : IEntityTypeConfiguration<BookingConfirmedInboxItem>
{
    public void Configure(EntityTypeBuilder<BookingConfirmedInboxItem> builder)
    {
        builder
            .ToTable(TableNames.BookingConfirmedInbox)
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
