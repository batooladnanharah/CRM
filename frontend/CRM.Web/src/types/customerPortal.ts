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

// Portal-facing knowledge-base article shapes — no authorId/status, since
// every article the portal can see is Published by definition.
export interface CustomerKnowledgeBaseArticleListItem {
  id: string
  title: string
  slug: string
  tags: string[]
  publishedAtUtc: string
}

export interface CustomerKnowledgeBaseArticleDetails {
  id: string
  title: string
  slug: string
  body: string
  tags: string[]
  publishedAtUtc: string
}

export interface CustomerKnowledgeBaseArticleListResult {
  items: CustomerKnowledgeBaseArticleListItem[]
  total: number
  page: number
  pageSize: number
}

export interface CustomerKnowledgeBaseCategorySummary {
  id: string
  name: string
  description: string | null
  articleCount: number
}

// Portal search reuses the CRM-side search response shape (see
// KnowledgeBaseSearchItem/Response in types/knowledgeBase.ts); status is
// always null for portal callers since every result is Published by
// definition.
export type { KnowledgeBaseSearchItem, KnowledgeBaseSearchResponse } from '@/types/knowledgeBase'
