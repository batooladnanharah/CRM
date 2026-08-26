import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalTicketCreateView from '@/modules/customerPortal/views/PortalTicketCreateView.vue'
import { i18n } from '@/i18n'
import type { CustomerTicketDetails } from '@/types/customerPortal'

function makeTicketDetails(overrides: Partial<CustomerTicketDetails> = {}): CustomerTicketDetails {
  return {
    id: 'ticket-1',
    title: 'New Ticket',
    description: 'Details',
    status: 'Open',
    priority: 'Normal',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    messages: [],
    history: [],
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    creating: false,
    error: null as string | null,
    createTicket: vi.fn<() => Promise<CustomerTicketDetails>>(),
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
      { path: '/portal/tickets/new', name: 'portal-ticket-create', component: PortalTicketCreateView },
      { path: '/portal/tickets/:id', name: 'portal-ticket-details', component: { template: '<div />' } },
    ],
  })
}

async function mountView(router: Router = makeRouter()) {
  router.push('/portal/tickets/new')
  await router.isReady()

  return mount(PortalTicketCreateView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('PortalTicketCreateView', () => {
  it('renders the subject, description, and priority fields', async () => {
    const wrapper = await mountView()

    expect(wrapper.find('#portal-ticket-title').exists()).toBe(true)
    expect(wrapper.find('#portal-ticket-description').exists()).toBe(true)
    expect(wrapper.find('#portal-ticket-priority').exists()).toBe(true)
  })

  it('shows a required error when the subject is empty', async () => {
    const wrapper = await mountView()

    await wrapper.find('#portal-ticket-description').setValue('Some description')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Subject is required.')
    expect(fakeStore.createTicket).not.toHaveBeenCalled()
  })

  it('shows a required error when the description is empty', async () => {
    const wrapper = await mountView()

    await wrapper.find('#portal-ticket-title').setValue('Cannot log in')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Description is required.')
    expect(fakeStore.createTicket).not.toHaveBeenCalled()
  })

  it('submits and navigates to the ticket details route with the submitted flag', async () => {
    fakeStore.createTicket.mockResolvedValue(makeTicketDetails({ id: 'ticket-99' }))
    const router = makeRouter()
    const wrapper = await mountView(router)

    await wrapper.find('#portal-ticket-title').setValue('Cannot log in')
    await wrapper.find('#portal-ticket-description').setValue('Details about the issue.')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(fakeStore.createTicket).toHaveBeenCalledWith({
      title: 'Cannot log in',
      description: 'Details about the issue.',
      priority: 'Normal',
    })
    expect(router.currentRoute.value.path).toBe('/portal/tickets/ticket-99')
    expect(router.currentRoute.value.query.submitted).toBe('1')
  })

  it('disables the submit button while creating', async () => {
    fakeStore = makeFakeStore({ creating: true })
    const wrapper = await mountView()

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('shows a generic error when the store reports one', async () => {
    fakeStore = makeFakeStore({ error: 'errorSave' })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Something went wrong. Please try again.')
  })
})
