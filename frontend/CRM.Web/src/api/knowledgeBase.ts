import { apiRequest } from './http'
import type {
  CreateKnowledgeBaseArticlePayload,
  CreateKnowledgeBaseCategoryPayload,
  KnowledgeBaseArticle,
  KnowledgeBaseCategory,
  KnowledgeBaseListQuery,
  KnowledgeBaseSearchRequest,
  KnowledgeBaseSearchResponse,
  KnowledgeBaseSearchResult,
  UpdateKnowledgeBaseArticlePayload,
  UpdateKnowledgeBaseCategoryPayload,
} from '@/types/knowledgeBase'

function buildQueryString(params: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value))
    }
  }
  const queryString = search.toString()
  return queryString ? `?${queryString}` : ''
}

export function listArticles(query: KnowledgeBaseListQuery = {}): Promise<KnowledgeBaseSearchResult> {
  const path = `/knowledge-base/articles${buildQueryString({
    status: query.status,
    tag: query.tag,
    categoryId: query.categoryId,
    page: query.page,
    pageSize: query.pageSize,
  })}`
  return apiRequest<KnowledgeBaseSearchResult>(path, { method: 'GET' })
}

export function searchArticles(query: KnowledgeBaseSearchRequest): Promise<KnowledgeBaseSearchResponse> {
  const path = `/knowledge-base/articles/search${buildQueryString({
    q: query.q,
    categoryId: query.categoryId ?? undefined,
    includeDrafts: query.includeDrafts ? 'true' : undefined,
    page: query.page,
    pageSize: query.pageSize,
  })}`
  return apiRequest<KnowledgeBaseSearchResponse>(path, { method: 'GET' })
}

export function getArticle(id: string): Promise<KnowledgeBaseArticle> {
  return apiRequest<KnowledgeBaseArticle>(`/knowledge-base/articles/${id}`, { method: 'GET' })
}

export function getArticleBySlug(slug: string): Promise<KnowledgeBaseArticle> {
  return apiRequest<KnowledgeBaseArticle>(
    `/knowledge-base/articles/by-slug/${encodeURIComponent(slug)}`, { method: 'GET' },
  )
}

export function createArticle(payload: CreateKnowledgeBaseArticlePayload): Promise<KnowledgeBaseArticle> {
  return apiRequest<KnowledgeBaseArticle>('/knowledge-base/articles', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateArticle(
  id: string, payload: UpdateKnowledgeBaseArticlePayload,
): Promise<KnowledgeBaseArticle> {
  return apiRequest<KnowledgeBaseArticle>(`/knowledge-base/articles/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteArticle(id: string): Promise<void> {
  return apiRequest<void>(`/knowledge-base/articles/${id}`, { method: 'DELETE' })
}

export function publishArticle(id: string): Promise<KnowledgeBaseArticle> {
  return apiRequest<KnowledgeBaseArticle>(`/knowledge-base/articles/${id}/publish`, { method: 'POST' })
}

export function unpublishArticle(id: string): Promise<KnowledgeBaseArticle> {
  return apiRequest<KnowledgeBaseArticle>(`/knowledge-base/articles/${id}/unpublish`, { method: 'POST' })
}

export function listCategories(query: { activeOnly?: boolean } = {}): Promise<KnowledgeBaseCategory[]> {
  const path = `/knowledge-base/categories${buildQueryString({
    activeOnly: query.activeOnly ? 'true' : undefined,
  })}`
  return apiRequest<{ items: KnowledgeBaseCategory[] }>(path, { method: 'GET' }).then((res) => res.items)
}

export function getCategory(id: string): Promise<KnowledgeBaseCategory> {
  return apiRequest<KnowledgeBaseCategory>(`/knowledge-base/categories/${id}`, { method: 'GET' })
}

export function createCategory(payload: CreateKnowledgeBaseCategoryPayload): Promise<KnowledgeBaseCategory> {
  return apiRequest<KnowledgeBaseCategory>('/knowledge-base/categories', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateCategory(
  id: string, payload: UpdateKnowledgeBaseCategoryPayload,
): Promise<KnowledgeBaseCategory> {
  return apiRequest<KnowledgeBaseCategory>(`/knowledge-base/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function setCategoryStatus(id: string, isActive: boolean): Promise<KnowledgeBaseCategory> {
  return apiRequest<KnowledgeBaseCategory>(`/knowledge-base/categories/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive }),
  })
}
