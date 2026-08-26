export type AdminRole = 'admin' | 'agent' | 'customer'

export interface AdminUserListItem {
  id: string
  email: string
  name: string
  role: AdminRole | ''
  isDisabled: boolean
  createdAtUtc: string
}

export interface AdminUserDetail extends AdminUserListItem {
  customerId: string | null
}

export interface AdminUserListQuery {
  search?: string
  role?: AdminRole
  disabled?: boolean
  page?: number
  pageSize?: number
}

export interface AdminCreateUserRequest {
  email: string
  password: string
  name: string
  role: AdminRole
  customerId?: string
}

export interface AdminUpdateUserRequest {
  email: string
  name: string
  customerId?: string
}

export interface AuditLogEntry {
  id: string
  occurredAtUtc: string
  actorUserId: string | null
  actorEmail: string | null
  action: string
  targetType: string | null
  targetId: string | null
  ipAddress: string | null
  payloadJson: string | null
}

export interface AuditLogQuery {
  actorId?: string
  targetId?: string
  action?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}
