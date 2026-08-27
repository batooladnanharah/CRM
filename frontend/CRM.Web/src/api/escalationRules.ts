import { apiRequest } from './http'
import type { CreateEscalationRulePayload, EscalationRule, UpdateEscalationRulePayload } from '@/types/notifications'

export function listEscalationRules(): Promise<EscalationRule[]> {
  return apiRequest<EscalationRule[]>('/sla/escalation-rules', { method: 'GET' })
}

export function getEscalationRule(id: string): Promise<EscalationRule> {
  return apiRequest<EscalationRule>(`/sla/escalation-rules/${id}`, { method: 'GET' })
}

export function createEscalationRule(payload: CreateEscalationRulePayload): Promise<EscalationRule> {
  return apiRequest<EscalationRule>('/sla/escalation-rules', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateEscalationRule(id: string, payload: UpdateEscalationRulePayload): Promise<EscalationRule> {
  return apiRequest<EscalationRule>(`/sla/escalation-rules/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function activateEscalationRule(id: string): Promise<EscalationRule> {
  return apiRequest<EscalationRule>(`/sla/escalation-rules/${id}/activate`, { method: 'PATCH' })
}

export function deactivateEscalationRule(id: string): Promise<EscalationRule> {
  return apiRequest<EscalationRule>(`/sla/escalation-rules/${id}/deactivate`, { method: 'PATCH' })
}

export function deleteEscalationRule(id: string): Promise<void> {
  return apiRequest<void>(`/sla/escalation-rules/${id}`, { method: 'DELETE' })
}
