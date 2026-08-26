import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AiAvailabilityBadge from '@/components/ai/AiAvailabilityBadge.vue'
import { useAiStore } from '@/stores/ai'
import { i18n } from '@/i18n'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('AiAvailabilityBadge', () => {
  it('renders "Available" when the store reports available:true', () => {
    const store = useAiStore()
    store.status = { enabled: true, provider: 'Development', available: true }

    const wrapper = mount(AiAvailabilityBadge, { global: { plugins: [i18n] } })

    expect(wrapper.text()).toContain('Available')
    expect(wrapper.text()).not.toContain('Not configured')
  })

  it('renders "Not configured" when the store reports available:false', () => {
    const store = useAiStore()
    store.status = { enabled: false, provider: null, available: false }

    const wrapper = mount(AiAvailabilityBadge, { global: { plugins: [i18n] } })

    expect(wrapper.text()).toContain('Not configured')
    expect(wrapper.text()).not.toContain('Available')
  })

  it('renders "Not configured" when the store status has not loaded yet', () => {
    const wrapper = mount(AiAvailabilityBadge, { global: { plugins: [i18n] } })

    expect(wrapper.text()).toContain('Not configured')
  })
})
