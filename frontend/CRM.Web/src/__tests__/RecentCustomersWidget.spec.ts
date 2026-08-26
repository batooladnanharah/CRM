import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import RecentCustomersWidget from '@/modules/dashboard/components/RecentCustomersWidget.vue'
import { i18n } from '@/i18n'
import type { RecentCustomerEntry } from '@/types/dashboard'

const pushMock = vi.fn<(location: unknown) => void>()

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: pushMock }),
}))

const mountOptions = {
  global: {
    plugins: [i18n],
    stubs: { RouterLink: true },
  },
}

function makeCustomer(overrides: Partial<RecentCustomerEntry> = {}): RecentCustomerEntry {
  return {
    id: 'customer-1',
    name: 'Alice Johnson',
    lastInteractionAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

describe('RecentCustomersWidget', () => {
  it('renders customer names', () => {
    const wrapper = mount(RecentCustomersWidget, {
      props: { customers: [makeCustomer()], loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('Alice Johnson')
  })

  it('renders the empty state when there are no customers', () => {
    const wrapper = mount(RecentCustomersWidget, {
      props: { customers: [], loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('No recent customer activity.')
  })

  it('shows a loading skeleton', () => {
    const wrapper = mount(RecentCustomersWidget, {
      props: { customers: [], loading: true },
      ...mountOptions,
    })

    expect(wrapper.find('.skeleton').exists()).toBe(true)
  })

  it('navigates to the customer profile when an item is clicked', async () => {
    pushMock.mockReset()
    const wrapper = mount(RecentCustomersWidget, {
      props: { customers: [makeCustomer({ id: 'customer-99' })], loading: false },
      ...mountOptions,
    })

    await wrapper.find('.customer-item').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'customer-profile', params: { id: 'customer-99' } })
  })
})
