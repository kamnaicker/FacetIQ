using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace FacetIQ.Data.Configurations;

public class SubjectAttributeConfiguration:IEntityTypeConfiguration<SubjectAttribute>
{
    public void Configure(EntityTypeBuilder<SubjectAttribute> builder)
    {}
}
