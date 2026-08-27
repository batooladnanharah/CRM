import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useNotificationStore } from '@/stores/notification'

beforeEach(() => {
  setActivePinia(createPinia())
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('notification store', () => {
  it('push() returns an id and appends to notifications', () => {
    const store = useNotificationStore()

    const id = store.push({ message: 'Saved.' })

    expect(typeof id).toBe('string')
    expect(store.notifications).toHaveLength(1)
    expect(store.notifications[0]).toMatchObject({ id, message: 'Saved.', variant: 'info' })
  })

  it('uses the default duration for success/info/warning and a longer one for error', () => {
    const store = useNotificationStore()

    store.push({ message: 'a', variant: 'success' })
    store.push({ message: 'b', variant: 'info' })
    store.push({ message: 'c', variant: 'warning' })
    store.push({ message: 'd', variant: 'error' })

    const [success, info, warning, error] = store.notifications
    expect(success!.duration).toBe(4000)
    expect(info!.duration).toBe(4000)
    expect(warning!.duration).toBe(4000)
    expect(error!.duration).toBe(8000)
  })

  it('auto-dismisses after the configured duration', () => {
    const store = useNotificationStore()
    const id = store.push({ message: 'Saved.', variant: 'success' })

    vi.advanceTimersByTime(3999)
    expect(store.notifications.find((n) => n.id === id)).toBeDefined()

    vi.advanceTimersByTime(1)
    expect(store.notifications.find((n) => n.id === id)).toBeUndefined()
  })

  it('duration: 0 never auto-dismisses', () => {
    const store = useNotificationStore()
    store.push({ message: 'Sticky.', duration: 0 })

    vi.advanceTimersByTime(1_000_000)

    expect(store.notifications).toHaveLength(1)
  })

  it('dismiss(id) removes the notification and clears its timer', () => {
    const store = useNotificationStore()
    const id = store.push({ message: 'Saved.' })

    store.dismiss(id)

    expect(store.notifications).toEqual([])
    // No pending timer should fire and throw or resurrect the notification.
    vi.advanceTimersByTime(10_000)
    expect(store.notifications).toEqual([])
  })

  it('clear() empties all notifications and clears all timers', () => {
    const store = useNotificationStore()
    store.push({ message: 'a' })
    store.push({ message: 'b' })

    store.clear()

    expect(store.notifications).toEqual([])
    vi.advanceTimersByTime(10_000)
    expect(store.notifications).toEqual([])
  })

  it('evicts the oldest notification once more than MAX_VISIBLE (5) are queued', () => {
    const store = useNotificationStore()
    const ids = Array.from({ length: 6 }, (_, i) => store.push({ message: `msg-${i}`, duration: 0 }))

    expect(store.notifications).toHaveLength(5)
    expect(store.notifications.map((n) => n.id)).not.toContain(ids[0])
    expect(store.notifications.map((n) => n.id)).toEqual(ids.slice(1))
  })
})
