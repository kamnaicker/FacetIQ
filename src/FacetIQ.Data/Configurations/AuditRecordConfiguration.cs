using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.RequesterUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(record => record.RequestedAttributeKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(record => record.TransformParameter).HasMaxLength(64);

        builder.Property(record => record.JustifyingPrinciple).HasMaxLength(256);

        builder.Property(record => record.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(record => record.Channel).HasConversion<string>().HasMaxLength(32);
        builder.Property(record => record.Outcome).HasConversion<string>().HasMaxLength(32);
        builder.Property(record => record.DenyReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(record => record.Transform).HasConversion<string>().HasMaxLength(32);

        // No foreign keys are declared. An audit record must survive the deletion of the
        // subject and the norm it describes, so it holds their identifiers as plain values.
        builder.HasIndex(record => new { record.SubjectId, record.Timestamp });
    }
}
