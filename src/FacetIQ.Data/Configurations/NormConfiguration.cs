using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FacetIQ.Data.Configurations;

public class NormConfiguration:IEntityTypeConfiguration<Norm>
{
    public void Configure(EntityTypeBuilder<Norm> builder) 
    {}
}
