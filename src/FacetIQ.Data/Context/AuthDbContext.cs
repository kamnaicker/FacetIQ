using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FacetIQ.Data.Context
{
    public class AuthDbContext : IdentityDbContext<AppUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.HasDefaultSchema("auth");

            mb.Entity<PendingRegistration>(entity =>
            {
                entity.HasKey(pending => pending.Id);
                entity.Property(pending => pending.Email).HasMaxLength(256).IsRequired();
                entity.Property(pending => pending.NormalizedEmail).HasMaxLength(256).IsRequired();
                entity.HasIndex(pending => pending.NormalizedEmail);
                entity.Property(pending => pending.PasswordHash).IsRequired();
                entity.Property(pending => pending.CodeHash).IsRequired();
                entity.Property(pending => pending.CancellationTokenHash).IsRequired();
                entity.HasIndex(pending => pending.CancellationTokenHash).IsUnique();
            });
        }
    }
}
