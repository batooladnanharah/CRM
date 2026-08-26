namespace CRM.Api.Customers;

public class CustomerNote
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    // FK to the authoring user (Auth.Users.Id) — no navigation property, since
    // AuthDbContext and CustomerDbContext are separate DbContexts (same physical
    // database). Author display name is resolved via a separate AuthDbContext
    // query at read time, not a cross-context join.
    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
