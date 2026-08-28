import { apiRequest } from './http'
import type {
  CreateKnowledgeBaseArticlePayload,
  KnowledgeBaseArticle,
  KnowledgeBaseListQuery,
  KnowledgeBaseSearchQuery,
  KnowledgeBaseSearchResult,
  UpdateKnowledgeBaseArticlePayload,
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
    page: query.page,
    pageSize: query.pageSize,
  })}`
  return apiRequest<KnowledgeBaseSearchResult>(path, { method: 'GET' })
}

export function searchArticles(query: KnowledgeBaseSearchQuery): Promise<KnowledgeBaseSearchResult> {
  const path = `/knowledge-base/articles/search${buildQueryString({
    q: query.q,
    tag: query.tag,
    status: query.status,
    page: query.page,
    pageSize: query.pageSize,
  })}`
  return apiRequest<KnowledgeBaseSearchResult>(path, { method: 'GET' })
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
