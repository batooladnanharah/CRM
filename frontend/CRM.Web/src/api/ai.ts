import { apiRequest } from './http'
import type { AiResponse, AiStatus } from '@/types/ai'

export function getAiStatus(): Promise<AiStatus> {
  return apiRequest<AiStatus>('/ai/status', { method: 'GET' })
}

export function summariseTicket(ticketId: string, signal?: AbortSignal): Promise<AiResponse> {
  return apiRequest<AiResponse>(`/ai/tickets/${ticketId}/summary`, { method: 'POST', signal })
}
