export type KnowledgeBaseArticleStatus = 'Draft' | 'Published' | 'Archived'

export interface KnowledgeBaseCategory {
  id: string
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface KnowledgeBaseCategorySummary {
  id: string
  name: string
  description: string | null
  articleCount: number
}

export interface KnowledgeBaseArticleCategoryRef {
  id: string
  name: string
  isActive: boolean
}

export interface CreateKnowledgeBaseCategoryPayload {
  name: string
  description: string | null
}

export type UpdateKnowledgeBaseCategoryPayload = CreateKnowledgeBaseCategoryPayload

export interface KnowledgeBaseArticle {
  id: string
  title: string
  slug: string
  body: string
  tags: string[]
  status: KnowledgeBaseArticleStatus
  authorId: string
  categoryId: string
  category: KnowledgeBaseArticleCategoryRef | null
  createdAtUtc: string
  updatedAtUtc: string
  publishedAtUtc: string | null
}

export interface KnowledgeBaseSearchResult {
  items: KnowledgeBaseArticle[]
  total: number
}

export interface KnowledgeBaseListQuery {
  status?: KnowledgeBaseArticleStatus
  tag?: string
  categoryId?: string
  page?: number
  pageSize?: number
}

// --- Full-text search (title + content + category name) — CRM-66 ---

export interface KnowledgeBaseSearchCategory {
  id: string
  name: string
}

export interface KnowledgeBaseSearchItem {
  id: string
  title: string
  category: KnowledgeBaseSearchCategory
  excerpt: string
  status?: KnowledgeBaseArticleStatus | null
}

export interface KnowledgeBaseSearchResponse {
  items: KnowledgeBaseSearchItem[]
  page: number
  pageSize: number
  totalCount: number
}

export interface KnowledgeBaseSearchRequest {
  q: string
  categoryId?: string | null
  page?: number
  pageSize?: number
  includeDrafts?: boolean
}

export interface CreateKnowledgeBaseArticlePayload {
  title: string
  slug: string
  body: string
  tags: string[]
  status: KnowledgeBaseArticleStatus
  categoryId: string
}

export type UpdateKnowledgeBaseArticlePayload = CreateKnowledgeBaseArticlePayload
