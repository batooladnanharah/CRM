import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import TicketsListView from '@/modules/tickets/views/TicketsListView.vue'
import { i18n } from '@/i18n'
import type { TicketListItem, TicketPriority, TicketStatus } from '@/types/tickets'

const mountOptions = {
  global: {
    plugins: [i18n],
    stubs: { RouterLink: true },
  },
}

const pushMock = vi.fn<(location: unknown) => void>()

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: pushMock }),
  RouterLink: { name: 'RouterLink', template: '<a><slot /></a>' },
}))

function makeTicket(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'Normal',
    assigneeUserId: null,
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

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    items: [] as TicketListItem[],
    total: 0,
    page: 1,
    pageSize: 20,
    search: '',
    status: '' as TicketStatus | '',
    priority: '' as TicketPriority | '',
    loading: false,
    error: null as string | null,
    fetchList: vi.fn<() => Promise<void>>(),
    setSearch: vi.fn<(term: string) => void>(),
    setFilters: vi.fn<(partial: Record<string, unknown>) => void>(),
    setPage: vi.fn<(page: number) => void>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/tickets', () => ({
  useTicketsStore: () => fakeStore,
}))

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
  pushMock.mockReset()
})

describe('TicketsListView', () => {
  it('renders rows from the store', () => {
    fakeStore.items = [makeTicket()]
    fakeStore.total = 1

    const wrapper = mount(TicketsListView, mountOptions)

    expect(wrapper.text()).toContain('Cannot log in')
    expect(wrapper.text()).toContain('Alice Johnson')
  })

  it('shows the empty state when there are no items', () => {
    const wrapper = mount(TicketsListView, mountOptions)

    expect(wrapper.text()).toContain('No tickets found.')
  })

  it('shows the loading state', () => {
    fakeStore.loading = true

    const wrapper = mount(TicketsListView, mountOptions)

    expect(wrapper.text()).toContain('Loading')
  })

  it('shows the error state', () => {
    fakeStore.error = 'errorLoad'

    const wrapper = mount(TicketsListView, mountOptions)

    expect(wrapper.text()).toContain('Could not load tickets. Please try again.')
  })

  it('invokes store.setSearch when the search input changes', async () => {
    const wrapper = mount(TicketsListView, mountOptions)

    await wrapper.find('input[type="search"]').setValue('login')

    expect(fakeStore.setSearch).toHaveBeenCalledWith('login')
  })

  it('invokes store.setFilters when the status filter changes', async () => {
    fakeStore.items = [makeTicket()]
    const wrapper = mount(TicketsListView, mountOptions)

    const selects = wrapper.findAll('select')
    await selects[0]!.setValue('Closed')

    expect(fakeStore.setFilters).toHaveBeenCalledWith({ status: 'Closed' })
  })

  it('invokes store.setFilters when the priority filter changes', async () => {
    fakeStore.items = [makeTicket()]
    const wrapper = mount(TicketsListView, mountOptions)

    const selects = wrapper.findAll('select')
    await selects[1]!.setValue('High')

    expect(fakeStore.setFilters).toHaveBeenCalledWith({ priority: 'High' })
  })

  it('invokes store.setPage when Next is clicked', async () => {
    fakeStore.items = [makeTicket()]
    fakeStore.total = 50
    fakeStore.page = 1
    fakeStore.pageSize = 20

    const wrapper = mount(TicketsListView, mountOptions)

    const buttons = wrapper.findAll('button')
    const nextButton = buttons.find((b) => b.text() === 'Next')!
    await nextButton.trigger('click')

    expect(fakeStore.setPage).toHaveBeenCalledWith(2)
  })

  it('does not call setPage from Prev when already on page 1', () => {
    fakeStore.items = [makeTicket()]
    fakeStore.page = 1

    const wrapper = mount(TicketsListView, mountOptions)

    const buttons = wrapper.findAll('button')
    const prevButton = buttons.find((b) => b.text() === 'Previous')!
    expect(prevButton.attributes('disabled')).toBeDefined()
  })

  it('navigates to the ticket details route when a row is clicked', async () => {
    fakeStore.items = [makeTicket({ id: 'ticket-42' })]

    const wrapper = mount(TicketsListView, mountOptions)

    await wrapper.find('tbody tr').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'ticket-details', params: { id: 'ticket-42' } })
  })

  it('calls fetchList on mount', () => {
    mount(TicketsListView, mountOptions)

    expect(fakeStore.fetchList).toHaveBeenCalled()
  })
})
