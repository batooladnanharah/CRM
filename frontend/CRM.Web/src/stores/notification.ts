import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { Notification, NotificationInput, NotificationVariant } from '@/types/notifications'

const DEFAULT_DURATION_MS = 4000
const ERROR_DURATION_MS = 8000
const MAX_VISIBLE = 5

function defaultDurationFor(variant: NotificationVariant): number {
  return variant === 'error' ? ERROR_DURATION_MS : DEFAULT_DURATION_MS
}

function generateId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }
  return `${Date.now()}-${Math.random()}`
}

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<Notification[]>([])
  const timers = new Map<string, ReturnType<typeof setTimeout>>()

  function clearTimer(id: string) {
    const handle = timers.get(id)
    if (handle !== undefined) {
      clearTimeout(handle)
      timers.delete(id)
    }
  }

  // No dedup by default: repeated calls (e.g. a double-clicked "Save") each
  // queue their own toast so the user sees confirmation of every event.
  function push(input: NotificationInput): string {
    const variant = input.variant ?? 'info'
    const duration = input.duration ?? defaultDurationFor(variant)
    const id = generateId()

    const notification: Notification = {
      id,
      message: input.message,
      variant,
      title: input.title,
      duration,
      createdAt: Date.now(),
    }

    notifications.value.push(notification)

    if (notifications.value.length > MAX_VISIBLE) {
      const evicted = notifications.value.shift()
      if (evicted) {
        clearTimer(evicted.id)
      }
    }

    if (duration > 0) {
      timers.set(
        id,
        globalThis.setTimeout(() => dismiss(id), duration),
      )
    }

    return id
  }

  function dismiss(id: string) {
    clearTimer(id)
    notifications.value = notifications.value.filter((n) => n.id !== id)
  }

  function clear() {
    for (const id of timers.keys()) {
      clearTimeout(timers.get(id))
    }
    timers.clear()
    notifications.value = []
  }

  return { notifications, push, dismiss, clear }
})
