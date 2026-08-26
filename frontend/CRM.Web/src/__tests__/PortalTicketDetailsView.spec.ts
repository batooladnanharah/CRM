import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalTicketDetailsView from '@/modules/customerPortal/views/PortalTicketDetailsView.vue'
import { i18n } from '@/i18n'
import type { CustomerTicketDetails } from '@/types/customerPortal'

function makeTicketDetails(overrides: Partial<CustomerTicketDetails> = {}): CustomerTicketDetails {
  return {
    id: '1',
    title: 'Cannot log in',
    description: 'Login fails since this morning.',
    status: 'Open',
    priority: 'High',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-02T00:00:00Z',
    messages: [],
    history: [],
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    currentTicket: null as CustomerTicketDetails | null,
    loading: false,
    error: null as string | null,
    fetchTicket: vi.fn<() => Promise<void>>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/customerPortal', () => ({
  useCustomerPortalStore: () => fakeStore,
}))

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/portal/tickets', name: 'portal-tickets-list', component: { template: '<div />' } },
      { path: '/portal/tickets/:id', name: 'portal-ticket-details', component: PortalTicketDetailsView },
    ],
  })
}

async function mountView(path = '/portal/tickets/1', router: Router = makeRouter()) {
  router.push(path)
  await router.isReady()

  return mount(PortalTicketDetailsView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('PortalTicketDetailsView', () => {
  it('calls fetchTicket on mount with the route id', async () => {
    await mountView('/portal/tickets/ticket-42')

    expect(fakeStore.fetchTicket).toHaveBeenCalledWith('ticket-42')
  })

  it('shows the loading state', async () => {
    fakeStore = makeFakeStore({ loading: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading your tickets')
  })

  it('shows ticket details once loaded', async () => {
    fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Cannot log in')
    expect(wrapper.text()).toContain('Login fails since this morning.')
  })

  it('renders customer-visible messages', async () => {
    fakeStore = makeFakeStore({
      currentTicket: makeTicketDetails({
        messages: [{ id: 'm1', body: 'We are looking into this.', createdAtUtc: '2026-01-01T12:00:00Z' }],
      }),
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('We are looking into this.')
  })

  it('shows the no-messages placeholder when there are none', async () => {
    fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('No replies yet.')
  })

  it('shows the success banner when navigated with submitted=1', async () => {
    fakeStore = makeFakeStore({ currentTicket: makeTicketDetails({ id: 'ticket-99' }) })
    const router = makeRouter()
    router.push('/portal/tickets/ticket-99?submitted=1')
    await router.isReady()
    const wrapper = mount(PortalTicketDetailsView, { global: { plugins: [router, i18n] } })
    await flushPromises()

    expect(wrapper.text()).toContain('Your ticket has been submitted successfully. Ticket #ticket-99')
  })

  it('shows the error state with a retry action', async () => {
    fakeStore = makeFakeStore({ error: 'errorLoad' })
    const wrapper = await mountView()

    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')

    expect(fakeStore.fetchTicket).toHaveBeenCalledTimes(2)
  })
})
