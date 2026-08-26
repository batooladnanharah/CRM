import { apiRequest } from './http'
import type { PagedResult } from '@/types/customers'
import type {
  CreateTicketMessagePayload,
  CreateTicketPayload,
  EligibleAgent,
  EscalateTicketPayload,
  Ticket,
  TicketHistoryEntry,
  TicketListItem,
  TicketListQuery,
  TicketMessage,
  TicketPriority,
  TicketStatus,
} from '@/types/tickets'

export function listTickets(query: TicketListQuery = {}): Promise<PagedResult<TicketListItem>> {
  const params = new URLSearchParams()

  if (query.search?.trim()) params.set('search', query.search.trim())
  if (query.status !== undefined) params.set('status', query.status)
  if (query.priority !== undefined) params.set('priority', query.priority)
  if (query.assigneeId !== undefined) params.set('assigneeId', query.assigneeId)
  if (query.updatedSince !== undefined) params.set('updatedSince', query.updatedSince)
  if (query.page !== undefined) params.set('page', String(query.page))
  if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize))

  const queryString = params.toString()
  const path = queryString ? `/tickets?${queryString}` : '/tickets'

  return apiRequest<PagedResult<TicketListItem>>(path, { method: 'GET' })
}

export function getTicket(id: string): Promise<Ticket> {
  return apiRequest<Ticket>(`/tickets/${id}`, { method: 'GET' })
}

export function createTicket(payload: CreateTicketPayload): Promise<Ticket> {
  return apiRequest<Ticket>('/tickets', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function assignTicket(id: string, agentUserId: string | null): Promise<Ticket> {
  return apiRequest<Ticket>(`/tickets/${id}/assignment`, {
    method: 'PUT',
    body: JSON.stringify({ agentUserId }),
  })
}

export function changeTicketStatus(id: string, status: TicketStatus): Promise<Ticket> {
  return apiRequest<Ticket>(`/tickets/${id}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status }),
  })
}

export function changeTicketPriority(id: string, priority: TicketPriority): Promise<Ticket> {
  return apiRequest<Ticket>(`/tickets/${id}/priority`, {
    method: 'PUT',
    body: JSON.stringify({ priority }),
  })
}

export function fetchTicketHistory(id: string): Promise<TicketHistoryEntry[]> {
  return apiRequest<TicketHistoryEntry[]>(`/tickets/${id}/history`, { method: 'GET' })
}

export function fetchEligibleAgents(): Promise<EligibleAgent[]> {
  return apiRequest<EligibleAgent[]>('/tickets/eligible-agents', { method: 'GET' })
}

export function listTicketMessages(
  ticketId: string,
  page = 1,
  pageSize = 20,
): Promise<PagedResult<TicketMessage>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return apiRequest<PagedResult<TicketMessage>>(
    `/tickets/${ticketId}/messages?${params.toString()}`,
    { method: 'GET' },
  )
}

export function createTicketMessage(
  ticketId: string,
  payload: CreateTicketMessagePayload,
): Promise<TicketMessage> {
  return apiRequest<TicketMessage>(`/tickets/${ticketId}/messages`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function escalateTicket(id: string, payload: EscalateTicketPayload): Promise<Ticket> {
  return apiRequest<Ticket>(`/tickets/${id}/escalate`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
