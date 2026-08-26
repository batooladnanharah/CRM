namespace CRM.Api.Customers;

public class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;   // normalized: Trim().ToLowerInvariant() before every write/lookup
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
