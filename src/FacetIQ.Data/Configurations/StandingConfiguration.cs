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

        // Matches the norm's relationship column, since one is tested against the other.
        builder.Property(standing => standing.Value)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(standing => standing.Issuer)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(standing => standing.IssuerKind)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Resolution starts from the pair a request names, and only accepted rows are read.
        builder.HasIndex(standing => new { standing.SubjectId, standing.RequesterUserId });

        builder.HasData(SeedData.Standings);
    }
}
