using FacetIQ.Data.Seeding;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class SubjectAttributeConfiguration : IEntityTypeConfiguration<SubjectAttribute>
{
    public void Configure(EntityTypeBuilder<SubjectAttribute> builder)
    {
        builder.ToTable("subject_attributes");

        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.Key)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(attribute => attribute.Value)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(attribute => attribute.Label)
            .HasMaxLength(64);

        // Stored by name so reordering the enum cannot change existing rows.
        builder.Property(attribute => attribute.CollectedFor)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Lookups are by subject and key.
        builder.HasIndex(attribute => new { attribute.SubjectId, attribute.Key });

        builder.HasData(SeedData.Attributes);
    }
}
