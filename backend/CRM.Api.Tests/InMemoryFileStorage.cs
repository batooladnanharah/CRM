using System.Collections.Concurrent;
using CRM.Api.Customers.Attachments;

namespace CRM.Api.Tests;

// Test double for IFileStorage so attachment tests never touch disk.
public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public async Task SaveAsync(Stream content, string storageKey, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        _files[storageKey] = buffer.ToArray();
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        if (!_files.TryGetValue(storageKey, out var bytes))
        {
            throw new FileNotFoundException(storageKey);
        }
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        _files.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }
}
