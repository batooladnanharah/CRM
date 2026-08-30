import { apiRequest } from './http'
import type { CreateSlaPolicyPayload, SlaPolicy, UpdateSlaPolicyPayload } from '@/types/tickets'

export function listSlaPolicies(): Promise<SlaPolicy[]> {
  return apiRequest<SlaPolicy[]>('/sla/policies', { method: 'GET' })
}

export function getSlaPolicy(id: string): Promise<SlaPolicy> {
  return apiRequest<SlaPolicy>(`/sla/policies/${id}`, { method: 'GET' })
}

export function createSlaPolicy(payload: CreateSlaPolicyPayload): Promise<SlaPolicy> {
  return apiRequest<SlaPolicy>('/sla/policies', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateSlaPolicy(id: string, payload: UpdateSlaPolicyPayload): Promise<SlaPolicy> {
  return apiRequest<SlaPolicy>(`/sla/policies/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteSlaPolicy(id: string): Promise<void> {
  return apiRequest<void>(`/sla/policies/${id}`, { method: 'DELETE' })
}

export interface UpdateSlaPolicyStatusPayload {
  isActive: boolean
  isDefault?: boolean | null
}

export interface UpdateSlaPolicyStatusResult {
  policy: SlaPolicy
  warnings: string[]
}

export function updateSlaPolicyStatus(
  id: string,
  payload: UpdateSlaPolicyStatusPayload,
): Promise<UpdateSlaPolicyStatusResult> {
  return apiRequest<UpdateSlaPolicyStatusResult>(`/sla/policies/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}
