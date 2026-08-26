import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MyTasksWidget from '@/modules/dashboard/components/MyTasksWidget.vue'
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
    title: 'Follow up on refund',
    status: 'Open',
    priority: 'Urgent',
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

describe('MyTasksWidget', () => {
  it('renders rows for each task', () => {
    const wrapper = mount(MyTasksWidget, {
      props: { tickets: [makeTicket()], loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('Follow up on refund')
    expect(wrapper.text()).toContain('Urgent')
  })

  it('renders the empty state when the list is empty', () => {
    const wrapper = mount(MyTasksWidget, {
      props: { tickets: [], loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('No tasks pending.')
  })

  it('shows a loading skeleton', () => {
    const wrapper = mount(MyTasksWidget, {
      props: { tickets: [], loading: true },
      ...mountOptions,
    })

    expect(wrapper.find('.skeleton').exists()).toBe(true)
  })

  it('navigates to ticket details when a row is clicked', async () => {
    pushMock.mockReset()
    const wrapper = mount(MyTasksWidget, {
      props: { tickets: [makeTicket({ id: 'task-7' })], loading: false },
      ...mountOptions,
    })

    await wrapper.find('tbody tr').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'ticket-details', params: { id: 'task-7' } })
  })
})
