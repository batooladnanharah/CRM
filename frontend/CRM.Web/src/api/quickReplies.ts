import { apiRequest } from './http'
import type { CreateQuickReplyPayload, QuickReply, UpdateQuickReplyPayload } from '@/types/tickets'

export function listQuickReplies(search?: string): Promise<QuickReply[]> {
  const params = new URLSearchParams()
  if (search?.trim()) params.set('search', search.trim())

  const queryString = params.toString()
  const path = queryString ? `/quick-replies?${queryString}` : '/quick-replies'

  return apiRequest<QuickReply[]>(path, { method: 'GET' })
}

export function createQuickReply(payload: CreateQuickReplyPayload): Promise<QuickReply> {
  return apiRequest<QuickReply>('/quick-replies', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateQuickReply(id: string, payload: UpdateQuickReplyPayload): Promise<QuickReply> {
  return apiRequest<QuickReply>(`/quick-replies/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteQuickReply(id: string): Promise<void> {
  return apiRequest<void>(`/quick-replies/${id}`, { method: 'DELETE' })
}
