using Microsoft.EntityFrameworkCore;

namespace CRM.Api.KnowledgeBase;

public sealed class KnowledgeBaseDbContext(DbContextOptions<KnowledgeBaseDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeBaseArticle> Articles => Set<KnowledgeBaseArticle>();
    public DbSet<KnowledgeBaseCategory> Categories => Set<KnowledgeBaseCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeBaseArticle>(entity =>
        {
            entity.ToTable("KnowledgeBaseArticles");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Title).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Slug).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Body).IsRequired().HasMaxLength(20000);

            // Stored as a string so the column stays readable and
            // migration-friendly, matching the convention used for
            // Ticket.Status/Ticket.Priority.
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);

            // Tags map to Postgres text[] (Npgsql handles string[] natively);
            // the InMemory test provider stores it as a regular array column.
            entity.HasIndex(a => a.Slug).IsUnique();
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.Title);

            // Restrict delete: a category with existing articles can never be
            // hard-deleted out from under them — callers deactivate instead.
            entity.HasOne(a => a.Category)
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => a.CategoryId);
        });

        modelBuilder.Entity<KnowledgeBaseCategory>(entity =>
        {
            entity.ToTable("KnowledgeBaseCategories");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name).IsRequired().HasMaxLength(120);
            entity.Property(c => c.Description).HasMaxLength(1000);
            entity.Property(c => c.IsActive).HasDefaultValue(true);

            // Exact-name uniqueness at the DB level as a safety net; the
            // authoritative case-insensitive duplicate check happens in
            // application code (ToLowerInvariant comparison) since DB
            // collation can't be relied on to be case-insensitive.
            entity.HasIndex(c => c.Name).IsUnique();
        });
    }
}
