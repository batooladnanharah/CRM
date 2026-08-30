import { apiRequest } from './http'
import type {
  CreateCustomerTicketPayload,
  CustomerDashboard,
  CustomerKnowledgeBaseArticleDetails,
  CustomerKnowledgeBaseArticleListItem,
  CustomerKnowledgeBaseArticleListResult,
  CustomerKnowledgeBaseCategorySummary,
  CustomerTicketDetails,
  CustomerTicketListItem,
} from '@/types/customerPortal'
import type { KnowledgeBaseSearchResponse } from '@/types/knowledgeBase'

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

export function listPortalCategories(): Promise<CustomerKnowledgeBaseCategorySummary[]> {
  return apiRequest<CustomerKnowledgeBaseCategorySummary[]>('/customer/knowledge-base/categories', { method: 'GET' })
}

export function listPortalCategoryArticles(id: string): Promise<CustomerKnowledgeBaseArticleListItem[]> {
  return apiRequest<CustomerKnowledgeBaseArticleListItem[]>(
    `/customer/knowledge-base/categories/${id}/articles`, { method: 'GET' },
  )
}

// Portal search — never sends includeDrafts; the backend forces
// published-only, active-category-only results regardless.
export function searchPortalKnowledgeBase(
  query: { q: string; categoryId?: string | null; page?: number; pageSize?: number },
): Promise<KnowledgeBaseSearchResponse> {
  const params = new URLSearchParams()
  params.set('q', query.q)
  if (query.categoryId) {
    params.set('categoryId', query.categoryId)
  }
  if (query.page) {
    params.set('page', String(query.page))
  }
  if (query.pageSize) {
    params.set('pageSize', String(query.pageSize))
  }
  return apiRequest<KnowledgeBaseSearchResponse>(
    `/customer/knowledge-base/search?${params.toString()}`, { method: 'GET' },
  )
}
