import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalTicketsListView from '@/modules/customerPortal/views/PortalTicketsListView.vue'
import { i18n } from '@/i18n'
import type { CustomerTicketListItem } from '@/types/customerPortal'

function makeTicket(overrides: Partial<CustomerTicketListItem> = {}): CustomerTicketListItem {
  return {
    id: '1',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'Normal',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    tickets: [] as CustomerTicketListItem[],
    loading: false,
    error: null as string | null,
    fetchTickets: vi.fn<() => Promise<void>>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/customerPortal', () => ({
  useCustomerPortalStore: () => fakeStore,
}))

const pushMock = vi.fn<(location: unknown) => void>()

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>()
  return { ...actual, useRouter: () => ({ push: pushMock }) }
})

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/portal/tickets', name: 'portal-tickets-list', component: PortalTicketsListView },
      { path: '/portal/tickets/new', name: 'portal-ticket-create', component: { template: '<div />' } },
      { path: '/portal/tickets/:id', name: 'portal-ticket-details', component: { template: '<div />' } },
    ],
  })
}

async function mountView(router: Router = makeRouter()) {
  router.push('/portal/tickets')
  await router.isReady()

  return mount(PortalTicketsListView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
  pushMock.mockReset()
})

describe('PortalTicketsListView', () => {
  it('calls fetchTickets on mount', async () => {
    await mountView()

    expect(fakeStore.fetchTickets).toHaveBeenCalledOnce()
  })

  it('renders rows from the store', async () => {
    fakeStore = makeFakeStore({ tickets: [makeTicket({ title: 'Billing question' })] })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Billing question')
  })

  it('shows the loading state', async () => {
    fakeStore = makeFakeStore({ loading: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading your tickets')
  })

  it('shows the error state with a retry action', async () => {
    fakeStore = makeFakeStore({ error: 'errorLoad' })
    const wrapper = await mountView()

    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')

    expect(fakeStore.fetchTickets).toHaveBeenCalledTimes(2)
  })

  it('shows the empty state with a CTA when there are no tickets', async () => {
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('You have no tickets yet.')
    expect(wrapper.text()).toContain('Submit a Ticket')
  })

  it('navigates to ticket details when a row is clicked', async () => {
    fakeStore = makeFakeStore({ tickets: [makeTicket({ id: 'ticket-42' })] })
    const wrapper = await mountView()

    await wrapper.find('tbody tr').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'portal-ticket-details', params: { id: 'ticket-42' } })
  })
})
