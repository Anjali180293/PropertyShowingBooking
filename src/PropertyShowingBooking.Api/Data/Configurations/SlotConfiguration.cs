using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyShowingBooking.Domain.Entities;

namespace PropertyShowingBooking.Api.Data.Configurations;

public class SlotConfiguration : IEntityTypeConfiguration<Slot>
{
    public void Configure(EntityTypeBuilder<Slot> builder)
    {
        builder.ToTable("Slots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PropertyId).IsRequired();
        builder.Property(s => s.StartsAt).IsRequired();
        builder.Property(s => s.DurationMinutes).IsRequired();
        builder.Property(s => s.Capacity).IsRequired();
        builder.Property(s => s.SeatsBooked).IsRequired();

        builder.Ignore(s => s.SeatsRemaining);

        builder.HasIndex(s => new { s.PropertyId, s.StartsAt });

        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.Slot)
            .HasForeignKey(b => b.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
