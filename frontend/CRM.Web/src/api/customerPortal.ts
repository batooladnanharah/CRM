import { apiRequest } from './http'
import type {
  CreateCustomerTicketPayload,
  CustomerDashboard,
  CustomerKnowledgeBaseArticleDetails,
  CustomerKnowledgeBaseArticleListResult,
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

export function fetchPortalKnowledgeBaseArticles(
  page = 1, pageSize = 20,
): Promise<CustomerKnowledgeBaseArticleListResult> {
  return apiRequest<CustomerKnowledgeBaseArticleListResult>(
    `/customer/knowledge-base/articles?page=${page}&pageSize=${pageSize}`, { method: 'GET' },
  )
}

export function fetchPortalKnowledgeBaseArticle(id: string): Promise<CustomerKnowledgeBaseArticleDetails> {
  return apiRequest<CustomerKnowledgeBaseArticleDetails>(`/customer/knowledge-base/articles/${id}`, { method: 'GET' })
}
