import { ref } from 'vue'
import { defineStore } from 'pinia'
import { listNotifications, markAllNotificationsRead, markNotificationRead } from '@/api/notifications'
import type { AppNotification } from '@/types/notifications'

const DEFAULT_POLL_INTERVAL_MS = 60_000

export const useNotificationsStore = defineStore('notifications', () => {
  const items = ref<AppNotification[]>([])
  const unreadCount = ref(0)
  const loading = ref(false)
  const error = ref<string | null>(null)

  let pollTimer: ReturnType<typeof setInterval> | null = null
  let focusListenerAttached = false

  async function fetch() {
    loading.value = true
    error.value = null

    try {
      const response = await listNotifications()
      items.value = response.items
      unreadCount.value = response.unreadCount
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function markRead(id: string) {
    const target = items.value.find((n) => n.id === id)
    if (!target || target.isRead) {
      return
    }

    try {
      await markNotificationRead(id)
      target.isRead = true
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    } catch {
      error.value = 'errorSave'
      throw new Error('errorSave')
    }
  }

  async function markAllRead() {
    try {
      await markAllNotificationsRead()
      items.value = items.value.map((n) => ({ ...n, isRead: true }))
      unreadCount.value = 0
    } catch {
      error.value = 'errorSave'
      throw new Error('errorSave')
    }
  }

  function startPolling(intervalMs = DEFAULT_POLL_INTERVAL_MS) {
    stopPolling()
    pollTimer = setInterval(() => {
      void fetch()
    }, intervalMs)

    if (!focusListenerAttached && typeof window !== 'undefined') {
      window.addEventListener('focus', onWindowFocus)
      focusListenerAttached = true
    }
  }

  function stopPolling() {
    if (pollTimer !== null) {
      clearInterval(pollTimer)
      pollTimer = null
    }
    if (focusListenerAttached && typeof window !== 'undefined') {
      window.removeEventListener('focus', onWindowFocus)
      focusListenerAttached = false
    }
  }

  function onWindowFocus() {
    void fetch()
  }

  return { items, unreadCount, loading, error, fetch, markRead, markAllRead, startPolling, stopPolling }
})
