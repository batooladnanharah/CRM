import { apiRequest } from './http'
import type { NotificationListResponse } from '@/types/notifications'

export function listNotifications(take = 50): Promise<NotificationListResponse> {
  return apiRequest<NotificationListResponse>(`/notifications?take=${take}`, { method: 'GET' })
}

export function markNotificationRead(id: string): Promise<void> {
  return apiRequest<void>(`/notifications/${id}/read`, { method: 'PATCH' })
}

export function markAllNotificationsRead(): Promise<void> {
  return apiRequest<void>('/notifications/read-all', { method: 'PATCH' })
}
