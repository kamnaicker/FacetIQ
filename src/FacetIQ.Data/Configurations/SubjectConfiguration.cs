using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class SubjectConfiguration:IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder) {}
}
