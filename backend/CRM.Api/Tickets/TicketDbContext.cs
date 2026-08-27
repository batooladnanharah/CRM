using CRM.Api.Sla;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Tickets;

public sealed class TicketDbContext(DbContextOptions<TicketDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketHistoryEntry> TicketHistory => Set<TicketHistoryEntry>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<MessageMention> MessageMentions => Set<MessageMention>();

    // SlaPolicy lives here (not its own DbContext) — this module already owns
    // every ticket-adjacent table, and SLA policies are meaningless without
    // the Ticket rows they apply to.
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();

    // Escalation rules/events (CRM-63) live here too — same rationale as
    // SlaPolicy: they are meaningless without the Ticket rows they escalate.
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<EscalationEvent> EscalationEvents => Set<EscalationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(4000);

            // Stored as strings so the columns stay readable and migration-friendly,
            // matching the convention used for CustomerInteraction.Type.
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(32);

            entity.HasIndex(t => t.CustomerId);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.CreatedAtUtc);

            // Tickets live in a separate DbContext from Customers (same physical
            // database via ConnectionStrings:CrmDb) — no cross-context FK/navigation
            // is possible, so CustomerId is a plain column with no relational constraint.
        });

        modelBuilder.Entity<TicketHistoryEntry>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.ChangeType).HasConversion<string>().HasMaxLength(32);
            entity.Property(h => h.OldValue).HasMaxLength(200);
            entity.Property(h => h.NewValue).HasMaxLength(200);
            entity.Property(h => h.Reason).HasMaxLength(500);

            entity.HasIndex(h => h.TicketId);

            // No navigation property to Ticket — same one-directional-FK style as
            // the rest of this context; no relational FK constraint either, since
            // there's nothing enforcing referential integrity across concerns here
            // (matches CustomerId on Ticket itself).
        });

        modelBuilder.Entity<TicketMessage>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Body)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(m => m.Channel).HasConversion<string>().HasMaxLength(16);

            entity.HasIndex(m => new { m.TicketId, m.CreatedAtUtc });

            // Unlike CustomerId on Ticket, TicketId here is a same-context FK
            // (both entities live in TicketDbContext), so it gets a real
            // relational constraint with cascade delete.
            entity.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketAttachment>(entity =>
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

            entity.HasIndex(a => new { a.TicketId, a.CreatedAtUtc });

            entity.HasOne<Ticket>()
                .WithMany()
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageMention>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.HasIndex(m => new { m.MessageId, m.UserId }).IsUnique();

            // Same-context FK to TicketMessage (both live in TicketDbContext),
            // so it gets a real relational constraint with cascade delete.
            entity.HasOne<TicketMessage>()
                .WithMany()
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SlaPolicy>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Channel).HasMaxLength(200);
            entity.Property(p => p.Priority).HasConversion<string>().HasMaxLength(32);

            entity.HasIndex(p => new { p.Priority, p.Channel, p.IsActive });

            // Filtered unique index: only ever one row with IsDefault = true.
            // (Npgsql renders this as a partial index; the EF Core InMemory
            // test provider ignores HasFilter but that's fine — the app-level
            // enforcement in SlaPolicyEndpoints.cs is the real guard there.)
            entity.HasIndex(p => p.IsDefault)
                .IsUnique()
                .HasFilter("\"IsDefault\" = true");
        });

        modelBuilder.Entity<EscalationRule>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name).IsRequired().HasMaxLength(128);
            entity.Property(r => r.Trigger).HasConversion<int>();

            entity.HasIndex(r => r.IsActive);
        });

        modelBuilder.Entity<EscalationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Trigger).HasConversion<int>();
            entity.Property(e => e.Objective).HasConversion<int>();

            // Dedupe guard — see EscalationDispatcher. (EF Core InMemory test
            // provider enforces unique indexes too, unlike filtered indexes.)
            entity.HasIndex(e => new { e.TicketId, e.RuleId, e.Trigger, e.Objective }).IsUnique();
        });
    }
}
