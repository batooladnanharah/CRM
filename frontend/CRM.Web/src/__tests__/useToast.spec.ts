import { describe, it, expect, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useToast } from '@/composables/useToast'
import { useNotificationStore } from '@/stores/notification'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('useToast', () => {
  it('success/error/warning/info forward to the store with the correct variant for a string message', () => {
    const toast = useToast()
    const store = useNotificationStore()

    toast.success('ok')
    toast.error('nope')
    toast.warning('careful')
    toast.info('fyi')

    expect(store.notifications.map((n) => [n.variant, n.message])).toEqual([
      ['success', 'ok'],
      ['error', 'nope'],
      ['warning', 'careful'],
      ['info', 'fyi'],
    ])
  })

  it('accepts a NotificationInput object and applies the variant', () => {
    const toast = useToast()
    const store = useNotificationStore()

    toast.error({ message: 'Failed.', title: 'Oops', duration: 0 })

    expect(store.notifications[0]).toMatchObject({
      variant: 'error',
      message: 'Failed.',
      title: 'Oops',
      duration: 0,
    })
  })

  it('dismiss and clear delegate to the store', () => {
    const toast = useToast()
    const store = useNotificationStore()

    const id = toast.success('ok')
    toast.dismiss(id)
    expect(store.notifications).toEqual([])

    toast.success('a')
    toast.success('b')
    toast.clear()
    expect(store.notifications).toEqual([])
  })
})
