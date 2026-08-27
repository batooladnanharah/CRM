import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useNotificationsStore } from '@/stores/notifications'
import type { listNotifications, markAllNotificationsRead, markNotificationRead } from '@/api/notifications'
import type { AppNotification, NotificationListResponse } from '@/types/notifications'

const { listMock, markReadMock, markAllReadMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listNotifications>(),
  markReadMock: vi.fn<typeof markNotificationRead>(),
  markAllReadMock: vi.fn<typeof markAllNotificationsRead>(),
}))

vi.mock('@/api/notifications', () => ({
  listNotifications: listMock,
  markNotificationRead: markReadMock,
  markAllNotificationsRead: markAllReadMock,
}))

function makeNotification(overrides: Partial<AppNotification> = {}): AppNotification {
  return {
    id: '1',
    type: 'SlaAtRisk',
    title: 'SLA At Risk',
    message: 'Ticket #1 is approaching its Response SLA.',
    ticketId: 'ticket-1',
    isRead: false,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeResponse(items: AppNotification[], unreadCount?: number): NotificationListResponse {
  return { items, unreadCount: unreadCount ?? items.filter((i) => !i.isRead).length }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.useFakeTimers()
  listMock.mockReset()
  markReadMock.mockReset()
  markAllReadMock.mockReset()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('notifications store', () => {
  it('has the expected initial state', () => {
    const store = useNotificationsStore()

    expect(store.items).toEqual([])
    expect(store.unreadCount).toBe(0)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() populates items and unreadCount', async () => {
    const notification = makeNotification()
    listMock.mockResolvedValue(makeResponse([notification]))

    const store = useNotificationsStore()
    await store.fetch()

    expect(store.items).toEqual([notification])
    expect(store.unreadCount).toBe(1)
    expect(store.error).toBeNull()
  })

  it('fetch() sets an error and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useNotificationsStore()
    await expect(store.fetch()).resolves.toBeUndefined()

    expect(store.error).toBeTruthy()
  })

  it('markRead() marks the item read and decrements unreadCount', async () => {
    listMock.mockResolvedValue(makeResponse([makeNotification({ id: '1', isRead: false })], 1))
    const store = useNotificationsStore()
    await store.fetch()

    markReadMock.mockResolvedValue(undefined)
    await store.markRead('1')

    expect(store.items[0]?.isRead).toBe(true)
    expect(store.unreadCount).toBe(0)
  })

  it('markAllRead() clears unreadCount and marks all items read', async () => {
    listMock.mockResolvedValue(
      makeResponse([makeNotification({ id: '1' }), makeNotification({ id: '2' })], 2),
    )
    const store = useNotificationsStore()
    await store.fetch()

    markAllReadMock.mockResolvedValue(undefined)
    await store.markAllRead()

    expect(store.unreadCount).toBe(0)
    expect(store.items.every((n) => n.isRead)).toBe(true)
  })

  it('startPolling() schedules periodic fetch calls', async () => {
    listMock.mockResolvedValue(makeResponse([]))
    const store = useNotificationsStore()

    store.startPolling(1000)
    await vi.advanceTimersByTimeAsync(1000)
    expect(listMock).toHaveBeenCalledTimes(1)

    await vi.advanceTimersByTimeAsync(1000)
    expect(listMock).toHaveBeenCalledTimes(2)

    store.stopPolling()
    await vi.advanceTimersByTimeAsync(2000)
    expect(listMock).toHaveBeenCalledTimes(2)
  })
})
