using Microsoft.EntityFrameworkCore;

namespace CRM.Api.KnowledgeBase;

public sealed class KnowledgeBaseDbContext(DbContextOptions<KnowledgeBaseDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeBaseArticle> Articles => Set<KnowledgeBaseArticle>();

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
        });
    }
}
