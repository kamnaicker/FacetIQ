using FacetIQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Context;

public class FacetIQDbContext : DbContext
{
    public FacetIQDbContext(DbContextOptions<FacetIQDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<SubjectAttribute> SubjectAttributes => Set<SubjectAttribute>();

    public DbSet<Norm> Norms => Set<Norm>();

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.HasDefaultSchema("app");

        mb.ApplyConfigurationsFromAssembly(typeof(FacetIQDbContext).Assembly);
    }
}
