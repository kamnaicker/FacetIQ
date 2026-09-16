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

        // Composite so a later edit path can add revisions as new rows.
        builder.HasKey(norm => new { norm.Id, norm.Version });

        builder.Property(norm => norm.Relationship).HasMaxLength(64);

        builder.Property(norm => norm.TransformParameter).HasMaxLength(64);

        builder.Property(norm => norm.JustifyingPrinciple)
            .HasMaxLength(256)
            .IsRequired();

        // Stored by name so reordering an enum cannot change the meaning of existing rows.
        builder.Property(norm => norm.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.Action).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.Transform).HasConversion<string>().HasMaxLength(32);
        builder.Property(norm => norm.DenyReason).HasConversion<string>().HasMaxLength(32);

        builder.HasOne(norm => norm.Attribute)
            .WithMany()
            .HasForeignKey(norm => norm.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Every lookup filters on subject and norms in force.
        builder.HasIndex(norm => new { norm.SubjectId, norm.SupersededAt });

        builder.HasData(SeedData.Norms);
    }
}
