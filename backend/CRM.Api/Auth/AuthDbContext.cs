using CRM.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Auth;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    // AuditLog lives here (not its own DbContext) — the audit trail is
    // meaningless without the Users table it references, and every writer
    // already holds an AuthDbContext.
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();

            // Roles is mapped as a native PostgreSQL text[] column. Other providers
            // (used by tests, e.g. the EF Core InMemory provider) fall back to the
            // provider's default handling of the CLR collection type.
            if (Database.IsNpgsql())
            {
                entity.Property(u => u.Roles).HasColumnType("text[]");
            }
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(64);
            entity.Property(a => a.TargetType).HasMaxLength(64);
            entity.Property(a => a.TargetId).HasMaxLength(128);
            entity.Property(a => a.IpAddress).HasMaxLength(64);
            entity.Property(a => a.UserAgent).HasMaxLength(512);

            entity.HasIndex(a => a.OccurredAtUtc);
            entity.HasIndex(a => a.ActorUserId);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => new { a.TargetType, a.TargetId });
        });
    }
}
