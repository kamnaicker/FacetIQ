using FacetIQ.Data.Seeding;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class NormConfiguration : IEntityTypeConfiguration<Norm>
{
    public void Configure(EntityTypeBuilder<Norm> builder)
    {
        builder.ToTable("norms");

        // Identity and revision together, because editing a norm creates a new row rather
        // than altering the existing one.
        builder.HasKey(norm => new { norm.Id, norm.Version });

        builder.Property(norm => norm.Relationship).HasMaxLength(64);

        builder.Property(norm => norm.TransformParameter).HasMaxLength(64);

        builder.Property(norm => norm.JustifyingPrinciple)
            .HasMaxLength(256)
            .IsRequired();

        // Enums are stored by name so a migration that reorders them cannot silently
        // reinterpret rules already written.
        builder.Property(norm => norm.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.Channel).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.Action).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.Transform).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.DenyReason).HasConversion<string>().HasMaxLength(32);

        builder.HasOne(norm => norm.Attribute)
            .WithMany()
            .HasForeignKey(norm => norm.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Matching starts from the subject's norms in force.
        builder.HasIndex(norm => new { norm.SubjectId, norm.SupersededAt });

        builder.HasData(SeedData.Norms);
    }
}
