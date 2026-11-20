using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyShowingBooking.Domain.Entities;

namespace PropertyShowingBooking.Api.Data.Configurations;

public class BookingHistoryConfiguration : IEntityTypeConfiguration<BookingHistory>
{
    public void Configure(EntityTypeBuilder<BookingHistory> builder)
    {
        builder.ToTable("BookingHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.BookingId).IsRequired();
        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(h => h.ToStatus).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(h => h.Actor).IsRequired().HasMaxLength(256);
        builder.Property(h => h.Reason).HasMaxLength(1024);
        builder.Property(h => h.ChangedAt).IsRequired();

        builder.HasIndex(h => h.BookingId);
        builder.HasIndex(h => h.ChangedAt);
    }
}
