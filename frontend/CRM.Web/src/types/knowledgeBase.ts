export type KnowledgeBaseArticleStatus = 'Draft' | 'Published' | 'Archived'

export interface KnowledgeBaseArticle {
  id: string
  title: string
  slug: string
  body: string
  tags: string[]
  status: KnowledgeBaseArticleStatus
  authorId: string
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
  page?: number
  pageSize?: number
}

export interface KnowledgeBaseSearchQuery {
  q: string
  tag?: string
  status?: KnowledgeBaseArticleStatus
  page?: number
  pageSize?: number
}

export interface CreateKnowledgeBaseArticlePayload {
  title: string
  slug: string
  body: string
  tags: string[]
  status: KnowledgeBaseArticleStatus
}

export type UpdateKnowledgeBaseArticlePayload = CreateKnowledgeBaseArticlePayload
