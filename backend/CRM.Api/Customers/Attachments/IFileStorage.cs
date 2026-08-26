namespace CRM.Api.Customers.Attachments;

public interface IFileStorage
{
    Task SaveAsync(Stream content, string storageKey, CancellationToken ct);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);
}
