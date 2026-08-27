import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ToastHost from '@/components/ui/ToastHost.vue'
import AppToast from '@/components/ui/AppToast.vue'
import { useNotificationStore } from '@/stores/notification'
import { i18n } from '@/i18n'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('ToastHost', () => {
  it('renders one AppToast per notification in store order', () => {
    const store = useNotificationStore()
    store.push({ message: 'first' })
    store.push({ message: 'second' })

    const wrapper = mount(ToastHost, { global: { plugins: [i18n] } })
    const toasts = wrapper.findAllComponents(AppToast)

    expect(toasts).toHaveLength(2)
    expect(toasts[0]!.props('notification').message).toBe('first')
    expect(toasts[1]!.props('notification').message).toBe('second')
  })

  it('dispatches dismiss on the store when a toast emits close', async () => {
    const store = useNotificationStore()
    const id = store.push({ message: 'first' })

    const wrapper = mount(ToastHost, { global: { plugins: [i18n] } })
    await wrapper.findComponent(AppToast).find('.app-toast__dismiss').trigger('click')

    expect(store.notifications.find((n) => n.id === id)).toBeUndefined()
  })

  it('carries role="region" and aria-live="polite" on the container', () => {
    const wrapper = mount(ToastHost, { global: { plugins: [i18n] } })

    expect(wrapper.attributes('role')).toBe('region')
    expect(wrapper.attributes('aria-live')).toBe('polite')
    expect(wrapper.attributes('aria-label')).toBe(i18n.global.t('notifications.regionLabel'))
  })
})
