using FacetIQ.Data.Seeding;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class StandingConfiguration : IEntityTypeConfiguration<Standing>
{
    public void Configure(EntityTypeBuilder<Standing> builder)
    {
        builder.ToTable("standings");

        builder.HasKey(standing => standing.Id);

        builder.Property(standing => standing.RequesterUserId)
            .HasMaxLength(450)
            .IsRequired();

        // Same length as Norm.Relationship, which it is matched against.
        builder.Property(standing => standing.Value)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(standing => standing.Issuer)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(standing => standing.IssuerKind)
            .HasConversion<string>()
            .HasMaxLength(32);

        // For GetAcceptedAsync.
        builder.HasIndex(standing => new { standing.SubjectId, standing.RequesterUserId });

        builder.HasData(SeedData.Standings);
    }
}
