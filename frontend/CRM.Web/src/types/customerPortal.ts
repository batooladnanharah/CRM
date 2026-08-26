import type { TicketPriority, TicketStatus } from '@/types/tickets'

export interface CustomerTicketListItem {
  id: string
  title: string
  status: TicketStatus
  priority: TicketPriority
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CustomerTicketMessage {
  id: string
  body: string
  createdAtUtc: string
}

export interface CustomerTicketHistoryEntry {
  id: string
  oldValue: string | null
  newValue: string | null
  changedAtUtc: string
}

export interface CustomerTicketDetails {
  id: string
  title: string
  description: string
  status: TicketStatus
  priority: TicketPriority
  createdAtUtc: string
  updatedAtUtc: string
  messages: CustomerTicketMessage[]
  history: CustomerTicketHistoryEntry[]
}

export interface CreateCustomerTicketPayload {
  title: string
  description: string
  priority?: TicketPriority
}

export interface CustomerDashboard {
  openCount: number
  pendingCount: number
  resolvedCount: number
  recentTickets: CustomerTicketListItem[]
}
