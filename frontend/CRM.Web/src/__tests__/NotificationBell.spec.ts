import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import NotificationBell from '@/components/notifications/NotificationBell.vue'
import { i18n } from '@/i18n'
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
    createdAt: new Date().toISOString(),
    ...overrides,
  }
}

function makeResponse(items: AppNotification[]): NotificationListResponse {
  return { items, unreadCount: items.filter((i) => !i.isRead).length }
}

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: { template: '<div />' } },
    { path: '/tickets/:id', name: 'ticket-details', component: { template: '<div />' } },
  ],
})

beforeEach(async () => {
  setActivePinia(createPinia())
  listMock.mockReset()
  markReadMock.mockReset()
  markAllReadMock.mockReset()
  await router.push('/')
})

function mountBell() {
  return mount(NotificationBell, { global: { plugins: [i18n, router] } })
}

describe('NotificationBell', () => {
  it('renders the unread badge with the unread count', async () => {
    listMock.mockResolvedValue(makeResponse([makeNotification({ isRead: false })]))

    const wrapper = mountBell()
    await flushPromises()

    expect(wrapper.find('.unread-badge').exists()).toBe(true)
    expect(wrapper.find('.unread-badge').text()).toBe('1')
  })

  it('hides the badge when unreadCount is 0', async () => {
    listMock.mockResolvedValue(makeResponse([makeNotification({ isRead: true })]))

    const wrapper = mountBell()
    await flushPromises()

    expect(wrapper.find('.unread-badge').exists()).toBe(false)
  })

  it('opens the popover on click and shows the empty state', async () => {
    listMock.mockResolvedValue(makeResponse([]))

    const wrapper = mountBell()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('You have no notifications.')
  })

  it('marks an item read and navigates to its ticket on click', async () => {
    listMock.mockResolvedValue(makeResponse([makeNotification({ id: '1', ticketId: 'ticket-1' })]))
    markReadMock.mockResolvedValue(undefined)
    const pushSpy = vi.spyOn(router, 'push')

    const wrapper = mountBell()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await flushPromises()

    await wrapper.find('.notification-item').trigger('click')
    await flushPromises()

    expect(markReadMock).toHaveBeenCalledWith('1')
    expect(pushSpy).toHaveBeenCalledWith('/tickets/ticket-1')
  })

  it('marks all as read when the footer button is clicked', async () => {
    listMock.mockResolvedValue(makeResponse([makeNotification({ id: '1' }), makeNotification({ id: '2' })]))
    markAllReadMock.mockResolvedValue(undefined)

    const wrapper = mountBell()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await flushPromises()

    const markAllButton = wrapper.findAll('button').find((b) => b.text() === 'Mark all as read')!
    await markAllButton.trigger('click')
    await flushPromises()

    expect(markAllReadMock).toHaveBeenCalledTimes(1)
  })

  it('shows an error state with retry option when loading fails', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const wrapper = mountBell()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Could not load notifications. Please try again.')
  })
})
