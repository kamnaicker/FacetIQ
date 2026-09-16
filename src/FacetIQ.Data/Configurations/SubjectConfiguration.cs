using FacetIQ.Data.Seeding;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");

        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.UserId)
            .HasMaxLength(450)
            .IsRequired();

        // One subject per account.
        builder.HasIndex(subject => subject.UserId).IsUnique();

        builder.HasMany(subject => subject.Attributes)
            .WithOne()
            .HasForeignKey(attribute => attribute.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(SeedData.Subjects);
    }
}
