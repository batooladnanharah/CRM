import { apiRequest } from './http'
import type { PagedResult } from '@/types/customers'
import type {
  AdminUserDetail,
  AdminUserListItem,
  AdminUserListQuery,
  AuditLogEntry,
  AuditLogQuery,
} from '@/types/security'

export function listUsers(query: AdminUserListQuery = {}): Promise<PagedResult<AdminUserListItem>> {
  const params = new URLSearchParams()

  if (query.search?.trim()) params.set('search', query.search.trim())
  if (query.role) params.set('role', query.role)
  if (query.disabled !== undefined) params.set('disabled', String(query.disabled))
  if (query.page !== undefined) params.set('page', String(query.page))
  if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize))

  const queryString = params.toString()
  const path = queryString ? `/admin/users?${queryString}` : '/admin/users'

  return apiRequest<PagedResult<AdminUserListItem>>(path, { method: 'GET' })
}

export function getUser(id: string): Promise<AdminUserDetail> {
  return apiRequest<AdminUserDetail>(`/admin/users/${id}`, { method: 'GET' })
}

export function assignRole(id: string, role: string): Promise<AdminUserDetail> {
  return apiRequest<AdminUserDetail>(`/admin/users/${id}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role }),
  })
}

export function disableUser(id: string): Promise<AdminUserDetail> {
  return apiRequest<AdminUserDetail>(`/admin/users/${id}/disable`, { method: 'POST' })
}

export function enableUser(id: string): Promise<AdminUserDetail> {
  return apiRequest<AdminUserDetail>(`/admin/users/${id}/enable`, { method: 'POST' })
}

export function listAuditLog(query: AuditLogQuery = {}): Promise<PagedResult<AuditLogEntry>> {
  const params = new URLSearchParams()

  if (query.actorId) params.set('actorId', query.actorId)
  if (query.targetId) params.set('targetId', query.targetId)
  if (query.action) params.set('action', query.action)
  if (query.from) params.set('from', query.from)
  if (query.to) params.set('to', query.to)
  if (query.page !== undefined) params.set('page', String(query.page))
  if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize))

  const queryString = params.toString()
  const path = queryString ? `/admin/audit-log?${queryString}` : '/admin/audit-log'

  return apiRequest<PagedResult<AuditLogEntry>>(path, { method: 'GET' })
}
