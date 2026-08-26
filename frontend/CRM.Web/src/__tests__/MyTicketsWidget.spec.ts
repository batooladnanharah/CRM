import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MyTicketsWidget from '@/modules/dashboard/components/MyTicketsWidget.vue'
import { i18n } from '@/i18n'
import type { TicketListItem } from '@/types/tickets'

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

function makeTicket(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'High',
    assigneeUserId: 'agent-1',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    sla: {
      policyId: null,
      firstResponseDueAtUtc: null,
      resolutionDueAtUtc: null,
      firstRespondedAtUtc: null,
      resolvedAtUtc: null,
      firstResponseStatus: 'NotApplicable',
      resolutionStatus: 'NotApplicable',
      firstResponseBreachedAtUtc: null,
      resolutionBreachedAtUtc: null,
      slaLastEvaluatedAtUtc: null,
      slaAutoEscalatedAtUtc: null,
    },
    ...overrides,
  }
}

describe('MyTicketsWidget', () => {
  it('renders rows for each ticket', () => {
    const wrapper = mount(MyTicketsWidget, {
      props: { tickets: [makeTicket()], loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('Cannot log in')
    expect(wrapper.text()).toContain('Alice Johnson')
    expect(wrapper.text()).toContain('High')
    expect(wrapper.text()).toContain('Open')
  })

  it('shows the empty state when there are no tickets', () => {
    const wrapper = mount(MyTicketsWidget, {
      props: { tickets: [], loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('You have no open tickets.')
  })

  it('shows a loading skeleton', () => {
    const wrapper = mount(MyTicketsWidget, {
      props: { tickets: [], loading: true },
      ...mountOptions,
    })

    expect(wrapper.find('.skeleton').exists()).toBe(true)
  })

  it('navigates to ticket details when a row is clicked', async () => {
    pushMock.mockReset()
    const wrapper = mount(MyTicketsWidget, {
      props: { tickets: [makeTicket({ id: 'ticket-42' })], loading: false },
      ...mountOptions,
    })

    await wrapper.find('tbody tr').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'ticket-details', params: { id: 'ticket-42' } })
  })
})
