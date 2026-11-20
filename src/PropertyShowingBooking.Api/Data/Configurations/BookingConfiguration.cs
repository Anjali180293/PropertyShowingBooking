using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyShowingBooking.Domain.Entities;

namespace PropertyShowingBooking.Api.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.SlotId).IsRequired();
        builder.Property(b => b.RenterEmail).IsRequired().HasMaxLength(320);
        builder.Property(b => b.PartySize).IsRequired();
        builder.Property(b => b.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(b => b.CreatedAt).IsRequired();

        builder.HasIndex(b => b.SlotId);
        builder.HasIndex(b => b.RenterEmail);

        builder.HasMany(b => b.History)
            .WithOne(h => h.Booking)
            .HasForeignKey(h => h.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
