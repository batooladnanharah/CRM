import type { TicketStatus } from '@/types/tickets'

// Mirrors backend/CRM.Api/Tickets/TicketStatusRules.cs — keep both in sync.
const ALLOWED_TRANSITIONS: Record<TicketStatus, TicketStatus[]> = {
  Open: ['InProgress', 'Resolved', 'Closed'],
  InProgress: ['Open', 'Resolved', 'Closed'],
  Resolved: ['Open', 'Closed'],
  Closed: [],
}

export function legalNextStatuses(current: TicketStatus): TicketStatus[] {
  return [current, ...ALLOWED_TRANSITIONS[current]]
}

// Mirrors the Resolved/Closed check in TicketEndpoints.cs's escalate handler —
// terminal for escalation purposes even though Resolved can still transition
// back to Open via the status endpoint.
export function isEscalatable(status: TicketStatus): boolean {
  return status !== 'Resolved' && status !== 'Closed'
}
