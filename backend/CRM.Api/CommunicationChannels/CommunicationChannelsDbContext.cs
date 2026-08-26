using Microsoft.EntityFrameworkCore;

namespace CRM.Api.CommunicationChannels;

public sealed class CommunicationChannelsDbContext(DbContextOptions<CommunicationChannelsDbContext> options)
    : DbContext(options)
{
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailMessageMetadata> EmailMessageMetadata => Set<EmailMessageMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Type).HasConversion<string>().HasMaxLength(32);

            entity.HasIndex(c => new { c.Type, c.Name }).IsUnique();
        });

        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.FromAddress).IsRequired().HasMaxLength(320);
            entity.Property(m => m.ToAddress).IsRequired().HasMaxLength(320);
            entity.Property(m => m.Subject).IsRequired().HasMaxLength(500);
            entity.Property(m => m.Body).IsRequired().HasMaxLength(20000);

            entity.HasIndex(m => m.ChannelId);
            entity.HasIndex(m => m.ReceivedAtUtc);

            // Same-context FK to Channel — deletion is blocked at the endpoint
            // level while emails exist (see CommunicationChannelEndpoints.cs),
            // so Restrict is a DB-level backstop rather than the primary rule.
            entity.HasOne<Channel>()
                .WithMany()
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmailMessageMetadata>(entity =>
        {
            entity.ToTable("EmailMessageMetadata");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.FromAddress).IsRequired().HasMaxLength(256);
            entity.Property(m => m.ToAddress).IsRequired().HasMaxLength(256);
            entity.Property(m => m.Subject).IsRequired().HasMaxLength(512);
            entity.Property(m => m.ProviderMessageId).HasMaxLength(256);
            entity.Property(m => m.DeliveryStatus).HasConversion<string>().HasMaxLength(16);

            entity.HasIndex(m => m.TicketMessageId).IsUnique();

            // No cross-context FK — TicketMessage lives in TicketDbContext, same
            // cross-context style as every other Guid link in this codebase
            // (e.g. Ticket.CustomerId).
        });
    }
}
