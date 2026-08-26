export interface Customer {
  id: string
  fullName: string
  email: string
  phone: string | null
  company: string | null
  createdAtUtc: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface CustomerListQuery {
  search?: string
  company?: string
  sortBy?: 'fullName' | 'email' | 'company' | 'createdAtUtc'
  sortDir?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}

export interface CreateCustomerPayload {
  fullName: string
  email: string
  phone?: string | null
  company?: string | null
}

export interface UpdateCustomerPayload {
  fullName: string
  email: string
  phone?: string | null
  company?: string | null
}

export type CustomerInteractionType =
  | 'TicketCreated'
  | 'CustomerMessage'
  | 'AgentReply'
  | 'StatusChange'
  | 'Assignment'
  | 'InternalNote'
  | 'Email'
  | 'WhatsApp'
  | 'LiveChat'
  | 'Sms'
  | 'WebForm'

export interface CustomerInteraction {
  id: string
  type: CustomerInteractionType
  summary: string
  occurredAt: string
  actorName?: string | null
  actorId?: string | null
  ticketId?: string | null
}

export interface CustomerNote {
  id: string
  customerId: string
  authorId: string
  authorDisplayName: string
  content: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateCustomerNotePayload {
  content: string
}

export interface UpdateCustomerNotePayload {
  content: string
}

export interface CustomerAttachment {
  id: string
  customerId: string
  originalFileName: string
  contentType: string
  fileSize: number
  uploadedByUserId: string
  uploadedByDisplayName: string
  createdAtUtc: string
}
