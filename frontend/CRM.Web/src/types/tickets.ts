export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed'
export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Urgent'

export type SlaStatus = 'NotApplicable' | 'OnTrack' | 'AtRisk' | 'Breached' | 'Met'

export interface TicketSla {
  policyId: string | null
  firstResponseDueAtUtc: string | null
  resolutionDueAtUtc: string | null
  firstRespondedAtUtc: string | null
  resolvedAtUtc: string | null
  firstResponseStatus: SlaStatus
  resolutionStatus: SlaStatus
  firstResponseBreachedAtUtc: string | null
  resolutionBreachedAtUtc: string | null
  slaLastEvaluatedAtUtc: string | null
  slaAutoEscalatedAtUtc: string | null
}

export interface TicketEscalation {
  agentNotified: boolean
  managerNotified: boolean
  trigger: string
  objective: string
}

export interface Ticket {
  id: string
  customerId: string
  customerName: string
  customerEmail: string
  title: string
  description: string
  status: TicketStatus
  priority: TicketPriority
  assigneeUserId: string | null
  assigneeDisplayName: string | null
  createdAtUtc: string
  updatedAtUtc: string
  sla: TicketSla
  escalations: TicketEscalation[]
  autoAssigned: boolean
}

export interface TicketListItem {
  id: string
  customerId: string
  customerName: string
  title: string
  status: TicketStatus
  priority: TicketPriority
  assigneeUserId: string | null
  createdAtUtc: string
  updatedAtUtc: string
  sla: TicketSla
}

export interface TicketListQuery {
  search?: string
  status?: TicketStatus
  priority?: TicketPriority
  assigneeId?: string
  updatedSince?: string
  page?: number
  pageSize?: number
}

export interface CreateTicketPayload {
  customerId: string
  title: string
  description: string
  priority?: TicketPriority
}

export interface EligibleAgent {
  id: string
  displayName: string
  email: string
}

export type TicketChangeType =
  | 'Assignment'
  | 'Status'
  | 'Priority'
  | 'MessageAdded'
  | 'AttachmentAdded'
  | 'AttachmentRemoved'
  | 'Escalated'
  | 'SlaRecalculated'
  | 'SlaBreached'
  | 'EmailSent'

export interface TicketHistoryEntry {
  id: string
  changeType: TicketChangeType
  oldValue: string | null
  newValue: string | null
  reason: string | null
  changedByUserId: string
  changedByDisplayName: string
  changedAtUtc: string
  isSystemActor: boolean
}

export type MessageChannel = 'Web' | 'Email'
export type EmailDeliveryStatus = 'Pending' | 'Sent' | 'Failed'

export interface TicketMessage {
  id: string
  ticketId: string
  authorUserId: string
  authorDisplayName: string
  body: string
  isInternal: boolean
  mentionedUserIds: string[]
  channel: MessageChannel
  emailDeliveryStatus: EmailDeliveryStatus | null
  createdAtUtc: string
}

export interface CreateTicketMessagePayload {
  body: string
  isInternal: boolean
  mentionedUserIds?: string[]
  channel?: MessageChannel
  subjectOverride?: string
}

export interface EmailDeliveryFailure {
  message: string
  messageId: string
}

export interface QuickReply {
  id: string
  title: string
  content: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateQuickReplyPayload {
  title: string
  content: string
}

export interface UpdateQuickReplyPayload {
  title: string
  content: string
  isActive: boolean
}

export interface TicketAttachment {
  id: string
  ticketId: string
  originalFileName: string
  contentType: string
  fileSize: number
  uploadedByUserId: string
  uploadedByDisplayName: string
  createdAtUtc: string
}

export interface EscalateTicketPayload {
  reason: string
}

export interface SlaPolicy {
  id: string
  name: string
  channel: string | null
  priority: TicketPriority
  firstResponseMinutes: number
  resolutionMinutes: number
  isDefault: boolean
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateSlaPolicyPayload {
  name: string
  channel?: string | null
  priority: TicketPriority
  firstResponseMinutes: number
  resolutionMinutes: number
  isDefault: boolean
  isActive: boolean
}

export interface UpdateSlaPolicyPayload {
  name: string
  channel?: string | null
  priority: TicketPriority
  firstResponseMinutes: number
  resolutionMinutes: number
  isDefault: boolean
  isActive: boolean
}
