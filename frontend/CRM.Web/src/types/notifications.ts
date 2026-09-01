export type NotificationVariant = 'success' | 'error' | 'warning' | 'info'

export interface NotificationInput {
  message: string
  variant?: NotificationVariant
  title?: string
  /** Milliseconds. 0 disables auto-dismiss. Undefined uses the variant default. */
  duration?: number
}

export interface Notification extends Required<Pick<NotificationInput, 'message'>> {
  id: string
  variant: NotificationVariant
  title?: string
  duration: number
  createdAt: number
}

// --- CRM-63: SLA escalation rules + in-app notification center ---
// Unrelated to the toast types above (Notification/NotificationInput) — these
// model the persisted /api/sla/escalation-rules and /api/notifications
// resources. Kept in the same file per story instructions (types/notifications.ts
// already existed for the toast types; extended rather than replaced).

export type EscalationTrigger = 'AtRisk' | 'Breached'

export interface EscalationRule {
  id: string
  name: string
  trigger: EscalationTrigger
  notifyAgent: boolean
  notifyManager: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateEscalationRulePayload {
  name: string
  trigger: EscalationTrigger
  notifyAgent: boolean
  notifyManager: boolean
  isActive: boolean
}

export type UpdateEscalationRulePayload = CreateEscalationRulePayload

export interface AppNotification {
  id: string
  type: 'SlaAtRisk' | 'SlaBreached' | 'CustomerReplied'
  title: string
  message: string
  ticketId?: string | null
  isRead: boolean
  createdAt: string
}

export interface NotificationListResponse {
  items: AppNotification[]
  unreadCount: number
}
