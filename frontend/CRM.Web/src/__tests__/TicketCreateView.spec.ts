import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import TicketCreateView from '@/modules/tickets/views/TicketCreateView.vue'
import TicketDetailsView from '@/modules/tickets/views/TicketDetailsView.vue'
import TicketsListView from '@/modules/tickets/views/TicketsListView.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import { useNotificationStore } from '@/stores/notification'
import type { createTicket } from '@/api/tickets'
import type { listCustomers } from '@/api/customers'
import type { Customer } from '@/types/customers'
import type { Ticket } from '@/types/tickets'

const { createTicketMock, listCustomersMock } = vi.hoisted(() => ({
  createTicketMock: vi.fn<typeof createTicket>(),
  listCustomersMock: vi.fn<typeof listCustomers>(),
}))

vi.mock('@/api/tickets', () => ({
  createTicket: createTicketMock,
}))

vi.mock('@/api/customers', () => ({
  listCustomers: listCustomersMock,
}))

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: 'customer-1',
    fullName: 'Alice Johnson',
    email: 'alice@example.com',
    phone: null,
    company: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: 'ticket-1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    customerEmail: 'alice@example.com',
    title: 'New Ticket',
    description: 'Details',
    status: 'Open',
    priority: 'Normal',
    assigneeUserId: null,
    assigneeDisplayName: null,
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
    escalations: [],
    autoAssigned: false,
    ...overrides,
  }
}

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/tickets', name: 'tickets-list', component: TicketsListView },
      { path: '/tickets/new', name: 'ticket-create', component: TicketCreateView },
      { path: '/tickets/:id', name: 'ticket-details', component: TicketDetailsView },
    ],
  })
}

async function mountCreateView(router: Router) {
  router.push('/tickets/new')
  await router.isReady()

  return mount(TicketCreateView, {
    global: {
      plugins: [router, i18n],
    },
  })
}

async function selectCustomer(wrapper: ReturnType<typeof mount>, customer: Customer) {
  listCustomersMock.mockResolvedValue({ items: [customer], page: 1, pageSize: 10, totalCount: 1 })
  await wrapper.find('#ticket-customer').setValue(customer.fullName)
  await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalled())
  await flushPromises()
  await wrapper.find('.customer-suggestions li').trigger('click')
}

beforeEach(() => {
  setActivePinia(createPinia())
  createTicketMock.mockReset()
  listCustomersMock.mockReset()
  listCustomersMock.mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0 })
})

describe('TicketCreateView', () => {
  it('renders the customer, title, description, priority fields and submit/cancel buttons', async () => {
    const wrapper = await mountCreateView(makeRouter())

    expect(wrapper.find('#ticket-customer').exists()).toBe(true)
    expect(wrapper.find('#ticket-title').exists()).toBe(true)
    expect(wrapper.find('#ticket-description').exists()).toBe(true)
    expect(wrapper.find('#ticket-priority').exists()).toBe(true)
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
    expect(wrapper.find('button[type="button"]').exists()).toBe(true)
  })

  it('shows a required error when no customer is selected', async () => {
    const wrapper = await mountCreateView(makeRouter())

    await wrapper.find('#ticket-title').setValue('Cannot log in')
    await wrapper.find('#ticket-description').setValue('Details')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Select a customer.')
    expect(createTicketMock).not.toHaveBeenCalled()
  })

  it('shows a required error for empty title after a customer is selected', async () => {
    const wrapper = await mountCreateView(makeRouter())
    await selectCustomer(wrapper, makeCustomer())

    await wrapper.find('#ticket-description').setValue('Details')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Title is required.')
    expect(createTicketMock).not.toHaveBeenCalled()
  })

  it('shows a required error for empty description', async () => {
    const wrapper = await mountCreateView(makeRouter())
    await selectCustomer(wrapper, makeCustomer())

    await wrapper.find('#ticket-title').setValue('Cannot log in')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Description is required.')
    expect(createTicketMock).not.toHaveBeenCalled()
  })

  it('calls createTicket with the selected customer and trimmed fields on valid submit', async () => {
    createTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountCreateView(makeRouter())
    await selectCustomer(wrapper, makeCustomer())

    await wrapper.find('#ticket-title').setValue('  Cannot log in  ')
    await wrapper.find('#ticket-description').setValue('  Details  ')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(createTicketMock).toHaveBeenCalledWith({
      customerId: 'customer-1',
      title: 'Cannot log in',
      description: 'Details',
      priority: 'Normal',
    })
  })

  it('disables the submit button while creating', async () => {
    let resolveCreate!: (value: Ticket) => void
    createTicketMock.mockReturnValue(
      new Promise((resolve) => {
        resolveCreate = resolve
      }),
    )

    const wrapper = await mountCreateView(makeRouter())
    await selectCustomer(wrapper, makeCustomer())
    await wrapper.find('#ticket-title').setValue('Cannot log in')
    await wrapper.find('#ticket-description').setValue('Details')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()

    resolveCreate!(makeTicket())
    await flushPromises()
  })

  it('navigates to the ticket details route on success', async () => {
    createTicketMock.mockResolvedValue(makeTicket({ id: 'ticket-99' }))

    const router = makeRouter()
    const wrapper = await mountCreateView(router)
    await selectCustomer(wrapper, makeCustomer())
    await wrapper.find('#ticket-title').setValue('Cannot log in')
    await wrapper.find('#ticket-description').setValue('Details')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/tickets/ticket-99')
  })

  it('includes the auto-assigned agent name in the success toast', async () => {
    createTicketMock.mockResolvedValue(
      makeTicket({
        id: 'ticket-42',
        assigneeUserId: 'agent-1',
        assigneeDisplayName: 'Sara Ahmed',
        autoAssigned: true,
      }),
    )

    const router = makeRouter()
    const wrapper = await mountCreateView(router)
    await selectCustomer(wrapper, makeCustomer())
    await wrapper.find('#ticket-title').setValue('Cannot log in')
    await wrapper.find('#ticket-description').setValue('Details')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    const notificationStore = useNotificationStore()
    expect(notificationStore.notifications.some((n) => n.message.includes('Sara Ahmed'))).toBe(true)
  })

  it('shows the customer_not_found message on a 400 and stays on the form', async () => {
    createTicketMock.mockRejectedValue(new ApiError(400, 'customer_not_found'))

    const router = makeRouter()
    const wrapper = await mountCreateView(router)
    await selectCustomer(wrapper, makeCustomer())
    await wrapper.find('#ticket-title').setValue('Cannot log in')
    await wrapper.find('#ticket-description').setValue('Details')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('The selected customer could not be found.')
    expect(router.currentRoute.value.path).toBe('/tickets/new')
  })

  it('navigates to the tickets list on Cancel without calling the API', async () => {
    const router = makeRouter()
    const wrapper = await mountCreateView(router)

    await wrapper.find('button[type="button"]').trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/tickets')
    expect(createTicketMock).not.toHaveBeenCalled()
  })
})
