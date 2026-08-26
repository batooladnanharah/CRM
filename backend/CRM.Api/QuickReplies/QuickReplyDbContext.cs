using Microsoft.EntityFrameworkCore;

namespace CRM.Api.QuickReplies;

public sealed class QuickReplyDbContext(DbContextOptions<QuickReplyDbContext> options) : DbContext(options)
{
    public DbSet<QuickReply> QuickReplies => Set<QuickReply>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuickReply>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.Title)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(q => q.Content)
                .IsRequired()
                .HasMaxLength(4000);

            entity.HasIndex(q => q.IsActive);

            // Case-insensitive substring search on Title/Content is done via
            // .ToLower().Contains() at query time (same convention as
            // CustomerEndpoints/TicketEndpoints) rather than a DB collation
            // trick, so no expression index is needed here.
            entity.HasIndex(q => q.Title);
        });
    }
}
