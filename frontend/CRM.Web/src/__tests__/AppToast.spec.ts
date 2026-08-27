import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AppToast from '@/components/ui/AppToast.vue'
import { i18n } from '@/i18n'
import type { Notification } from '@/types/notifications'

function makeNotification(overrides: Partial<Notification> = {}): Notification {
  return {
    id: '1',
    message: 'Saved successfully.',
    variant: 'success',
    duration: 4000,
    createdAt: 0,
    ...overrides,
  }
}

describe('AppToast', () => {
  it('renders the message and an optional title', () => {
    const wrapper = mount(AppToast, {
      props: { notification: makeNotification({ title: 'Success', message: 'Saved.' }) },
      global: { plugins: [i18n] },
    })

    expect(wrapper.text()).toContain('Success')
    expect(wrapper.text()).toContain('Saved.')
  })

  it('emits close with the notification id when the dismiss button is clicked', async () => {
    const wrapper = mount(AppToast, {
      props: { notification: makeNotification({ id: 'abc' }) },
      global: { plugins: [i18n] },
    })

    await wrapper.find('.app-toast__dismiss').trigger('click')

    expect(wrapper.emitted('close')).toEqual([['abc']])
  })

  it('sets role="alert" and aria-live="assertive" for the error variant', () => {
    const wrapper = mount(AppToast, {
      props: { notification: makeNotification({ variant: 'error' }) },
      global: { plugins: [i18n] },
    })

    expect(wrapper.attributes('role')).toBe('alert')
    expect(wrapper.attributes('aria-live')).toBe('assertive')
  })

  it('sets role="status" and aria-live="polite" for non-error variants', () => {
    const wrapper = mount(AppToast, {
      props: { notification: makeNotification({ variant: 'success' }) },
      global: { plugins: [i18n] },
    })

    expect(wrapper.attributes('role')).toBe('status')
    expect(wrapper.attributes('aria-live')).toBe('polite')
  })

  it('gives the close button an accessible name from i18n', () => {
    const wrapper = mount(AppToast, {
      props: { notification: makeNotification() },
      global: { plugins: [i18n] },
    })

    expect(wrapper.find('.app-toast__dismiss').attributes('aria-label')).toBe(i18n.global.t('notifications.close'))
  })
})
