using FacetIQ.Data.Configurations;
using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Context;

public class FacetIQDbContext : DbContext
{
    public FacetIQDbContext(DbContextOptions<FacetIQDbContext> options): base(options)
    {}

    protected FacetIQDbContext() 
    {}

    protected override void OnModelCreating(ModelBuilder model)
    {
        new AuditRecordConfiguration().Configure(model.Entity<AuditRecord>());
        new NormConfiguration().Configure(model.Entity<Norm>());
        new SubjectAttributeConfiguration().Configure(model.Entity<SubjectAttribute>());
        new SubjectConfiguration().Configure(model.Entity<Subject>());
    }
}
