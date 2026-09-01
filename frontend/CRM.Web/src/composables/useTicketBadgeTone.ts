import type { TicketPriority, TicketStatus } from '@/types/tickets'

type BadgeTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info'

const STATUS_TONES: Record<TicketStatus, BadgeTone> = {
  Open: 'info',
  InProgress: 'warning',
  Resolved: 'success',
  Closed: 'neutral',
}

const PRIORITY_TONES: Record<TicketPriority, BadgeTone> = {
  Low: 'neutral',
  Normal: 'info',
  High: 'warning',
  Urgent: 'danger',
}

export function useTicketBadgeTone() {
  function statusTone(status: TicketStatus): BadgeTone {
    return STATUS_TONES[status]
  }

  function priorityTone(priority: TicketPriority): BadgeTone {
    return PRIORITY_TONES[priority]
  }

  return { statusTone, priorityTone }
}
