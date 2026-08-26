import { apiRequest } from './http'
import type {
  CreateCustomerTicketPayload,
  CustomerDashboard,
  CustomerTicketDetails,
  CustomerTicketListItem,
} from '@/types/customerPortal'

export function fetchPortalDashboard(): Promise<CustomerDashboard> {
  return apiRequest<CustomerDashboard>('/customer/dashboard', { method: 'GET' })
}

export function fetchPortalTickets(): Promise<CustomerTicketListItem[]> {
  return apiRequest<CustomerTicketListItem[]>('/customer/tickets', { method: 'GET' })
}

export function fetchPortalTicket(id: string): Promise<CustomerTicketDetails> {
  return apiRequest<CustomerTicketDetails>(`/customer/tickets/${id}`, { method: 'GET' })
}

export function createPortalTicket(payload: CreateCustomerTicketPayload): Promise<CustomerTicketDetails> {
  return apiRequest<CustomerTicketDetails>('/customer/tickets', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
