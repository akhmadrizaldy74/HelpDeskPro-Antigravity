namespace HelpDeskPro.Infrastructure.Persistence.Configurations;

using HelpDeskPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Date)
            .IsRequired();

        // Unik hanya untuk data yang belum dihapus (soft delete), agar tanggal
        // hari libur yang pernah dihapus dapat didaftarkan kembali.
        builder.HasIndex(h => h.Date)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(h => h.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.CreatedBy)
            .HasMaxLength(100);

        builder.Property(h => h.UpdatedBy)
            .HasMaxLength(100);
    }
}
