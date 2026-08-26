using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Customers;

public sealed class CustomerDbContext(DbContextOptions<CustomerDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerInteraction> CustomerInteractions => Set<CustomerInteraction>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<CustomerAttachment> CustomerAttachments => Set<CustomerAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.FullName)
                .IsRequired()
                .HasMaxLength(200);

            // Email is normalized (Trim().ToLowerInvariant()) at every write/lookup
            // site, matching the existing Auth Users.Email pattern — a plain unique
            // index on the normalized column, no citext/expression index.
            entity.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(320);
            entity.HasIndex(c => c.Email).IsUnique();

            entity.Property(c => c.Phone).HasMaxLength(32);
            entity.Property(c => c.Company).HasMaxLength(200);
        });

        modelBuilder.Entity<CustomerInteraction>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Summary)
                .IsRequired()
                .HasMaxLength(1000);

            // Stored as a string so the column stays readable and migration-friendly.
            entity.Property(i => i.Type).HasConversion<string>().HasMaxLength(32);

            entity.Property(i => i.ActorName).HasMaxLength(200);

            entity.HasIndex(i => new { i.CustomerId, i.OccurredAt });

            // No navigation property on Customer — this is a one-directional FK only.
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerNote>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Content)
                .IsRequired()
                .HasMaxLength(4000);

            entity.HasIndex(n => new { n.CustomerId, n.CreatedAtUtc });

            // No navigation property on Customer — same one-directional FK style as CustomerInteraction.
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(n => n.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerAttachment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.OriginalFileName)
                .IsRequired()
                .HasMaxLength(260);

            entity.Property(a => a.StorageKey)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(a => a.ContentType)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasIndex(a => a.CustomerId);

            // No navigation property on Customer — same one-directional FK style as CustomerNote.
            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
