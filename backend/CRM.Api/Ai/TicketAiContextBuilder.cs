using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Ai;

public sealed record TicketAiContextMessage(string Body, bool IsInternal, DateTime CreatedAtUtc);

public sealed record TicketAiContext(
    string Subject,
    string Description,
    string Status,
    string Priority,
    IReadOnlyList<TicketAiContextMessage> Messages);

public interface ITicketAiContextBuilder
{
    Task<TicketAiContext?> BuildAsync(Guid ticketId, CancellationToken cancellationToken);
}

// Builds a scrubbed, size-bounded view of a ticket for AI prompts. Deliberately
// excludes anything not needed to summarise the conversation: auth tokens,
// credentials, customer contact fields (email/phone), attachment binary content,
// and any other ticket/customer field beyond subject/description/status/priority
// and the message bodies themselves.
public sealed class TicketAiContextBuilder(TicketDbContext ticketDb) : ITicketAiContextBuilder
{
    // Bounds how much conversation text ever reaches a prompt — matches the
    // existing per-request cap used before this story (AiApplicationService),
    // kept here so all AI features share one truncation policy.
    private const int MaxMessages = 30;
    private const int MaxMessageLength = 2000;

    public async Task<TicketAiContext?> BuildAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await ticketDb.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var messages = await ticketDb.TicketMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(MaxMessages)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new TicketAiContextMessage(m.Body, m.IsInternal, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var truncated = messages
            .Select(m => m with { Body = Truncate(m.Body, MaxMessageLength) })
            .ToList();

        return new TicketAiContext(
            ticket.Title,
            ticket.Description,
            ticket.Status.ToString(),
            ticket.Priority.ToString(),
            truncated);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
