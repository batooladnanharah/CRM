import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import CustomerProfileView from '@/modules/customers/views/CustomerProfileView.vue'
import CustomersListView from '@/modules/customers/views/CustomersListView.vue'
import CustomerEditView from '@/modules/customers/views/CustomerEditView.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type { getCustomer } from '@/api/customers'
import type { Customer } from '@/types/customers'
import { useCustomersStore } from '@/stores/customers'

const { getCustomerMock } = vi.hoisted(() => ({
  getCustomerMock: vi.fn<typeof getCustomer>(),
}))

vi.mock('@/api/customers', () => ({
  getCustomer: getCustomerMock,
  listCustomers: vi
    .fn<() => Promise<{ items: never[]; page: number; pageSize: number; totalCount: number }>>()
    .mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0 }),
  getCustomerInteractions: vi
    .fn<() => Promise<{ items: never[]; page: number; pageSize: number; totalCount: number }>>()
    .mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0 }),
  listCustomerNotes: vi.fn<() => Promise<never[]>>().mockResolvedValue([]),
}))

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: '1',
    fullName: 'Existing Customer',
    email: 'existing@example.com',
    phone: '+1-555-0100',
    company: 'Acme Corp',
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/customers', name: 'customers', component: CustomersListView },
      { path: '/customers/:id', name: 'customer-profile', component: CustomerProfileView },
      { path: '/customers/:id/edit', name: 'customer-edit', component: CustomerEditView },
    ],
  })
}

async function mountProfileView(router: Router, id = '1') {
  router.push(`/customers/${id}`)
  await router.isReady()

  return mount(CustomerProfileView, {
    global: {
      plugins: [router, i18n],
    },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  getCustomerMock.mockReset()
})

describe('CustomerProfileView', () => {
  it('renders a loading state before the customer resolves', async () => {
    let resolveGet!: (value: Customer) => void
    getCustomerMock.mockReturnValue(
      new Promise((resolve) => {
        resolveGet = resolve
      }),
    )

    const wrapper = await mountProfileView(makeRouter())

    expect(wrapper.text()).not.toContain('Existing Customer')

    resolveGet!(makeCustomer())
    await flushPromises()
  })

  it('renders the customer name, email, phone, company, and formatted created date', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountProfileView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Existing Customer')
    expect(wrapper.text()).toContain('existing@example.com')
    expect(wrapper.text()).toContain('+1-555-0100')
    expect(wrapper.text()).toContain('Acme Corp')
  })

  it('renders the missing placeholder for null optional fields', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer({ phone: null, company: null }))

    const wrapper = await mountProfileView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('—')
  })

  it('renders the not-found card and Back button on a 404', async () => {
    getCustomerMock.mockRejectedValue(new ApiError(404, 'Not found'))

    const wrapper = await mountProfileView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Customer not found.')
  })

  it('renders the error card with Retry on a generic failure, and Retry re-invokes getById', async () => {
    getCustomerMock.mockRejectedValue(new Error('network down'))

    const wrapper = await mountProfileView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Unable to load customer information.')

    getCustomerMock.mockResolvedValue(makeCustomer())
    const buttons = wrapper.findAll('button')
    const retryButton = buttons.find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')
    await flushPromises()

    expect(getCustomerMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Existing Customer')
  })

  it('navigates to the edit route when Edit is clicked', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const router = makeRouter()
    const wrapper = await mountProfileView(router)
    await flushPromises()

    const buttons = wrapper.findAll('button')
    const editButton = buttons.find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/customers/1/edit')
  })

  it('navigates to the customers list when Back is clicked', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const router = makeRouter()
    const wrapper = await mountProfileView(router)
    await flushPromises()

    const buttons = wrapper.findAll('button')
    const backButton = buttons.find((b) => b.text() === 'Back to Customers')!
    await backButton.trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/customers')
  })

  it('clears the current customer from the store on unmount', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const router = makeRouter()
    const wrapper = await mountProfileView(router)
    await flushPromises()

    const store = useCustomersStore()
    expect(store.current).not.toBeNull()

    wrapper.unmount()

    expect(store.current).toBeNull()
  })

  it('mounts the interaction timeline only when the Interactions tab is active', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountProfileView(makeRouter())
    await flushPromises()

    expect(wrapper.findComponent({ name: 'CustomerInteractionTimeline' }).exists()).toBe(false)

    const buttons = wrapper.findAll('button')
    const interactionsTab = buttons.find((b) => b.text() === 'Interactions')!
    await interactionsTab.trigger('click')
    await flushPromises()

    expect(wrapper.findComponent({ name: 'CustomerInteractionTimeline' }).exists()).toBe(true)

    const overviewTab = buttons.find((b) => b.text() === 'Overview')!
    await overviewTab.trigger('click')
    await flushPromises()

    expect(wrapper.findComponent({ name: 'CustomerInteractionTimeline' }).exists()).toBe(false)
  })

  it('mounts the notes section only when the Notes tab is active', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountProfileView(makeRouter())
    await flushPromises()

    expect(wrapper.findComponent({ name: 'CustomerNotesSection' }).exists()).toBe(false)

    const buttons = wrapper.findAll('button')
    const notesTab = buttons.find((b) => b.text() === 'Notes')!
    await notesTab.trigger('click')
    await flushPromises()

    expect(wrapper.findComponent({ name: 'CustomerNotesSection' }).exists()).toBe(true)

    const overviewTab = buttons.find((b) => b.text() === 'Overview')!
    await overviewTab.trigger('click')
    await flushPromises()

    expect(wrapper.findComponent({ name: 'CustomerNotesSection' }).exists()).toBe(false)
  })
})
