import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import CustomerEditView from '@/modules/customers/views/CustomerEditView.vue'
import CustomersListView from '@/modules/customers/views/CustomersListView.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type { getCustomer, updateCustomer } from '@/api/customers'
import type { Customer } from '@/types/customers'

const { getCustomerMock, updateCustomerMock } = vi.hoisted(() => ({
  getCustomerMock: vi.fn<typeof getCustomer>(),
  updateCustomerMock: vi.fn<typeof updateCustomer>(),
}))

vi.mock('@/api/customers', () => ({
  getCustomer: getCustomerMock,
  updateCustomer: updateCustomerMock,
  listCustomers: vi
    .fn<() => Promise<{ items: never[]; page: number; pageSize: number; totalCount: number }>>()
    .mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0 }),
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
      { path: '/customers/:id/edit', name: 'customer-edit', component: CustomerEditView },
    ],
  })
}

async function mountEditView(router: Router, id = '1') {
  router.push(`/customers/${id}/edit`)
  await router.isReady()

  return mount(CustomerEditView, {
    global: {
      plugins: [router, i18n],
    },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  getCustomerMock.mockReset()
  updateCustomerMock.mockReset()
})

describe('CustomerEditView', () => {
  it('loads and pre-fills the form from the existing customer', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountEditView(makeRouter())
    await flushPromises()

    expect((wrapper.find('#customer-fullName').element as HTMLInputElement).value).toBe('Existing Customer')
    expect((wrapper.find('#customer-email').element as HTMLInputElement).value).toBe('existing@example.com')
    expect((wrapper.find('#customer-phone').element as HTMLInputElement).value).toBe('+1-555-0100')
    expect((wrapper.find('#customer-company').element as HTMLInputElement).value).toBe('Acme Corp')
  })

  it('shows the loading state before the customer resolves', async () => {
    let resolveGet!: (value: Customer) => void
    getCustomerMock.mockReturnValue(
      new Promise((resolve) => {
        resolveGet = resolve
      }),
    )

    const wrapper = await mountEditView(makeRouter())

    expect(wrapper.text()).toContain('Loading customer…')

    resolveGet!(makeCustomer())
    await flushPromises()
  })

  it('shows the not-found state on a 404', async () => {
    getCustomerMock.mockRejectedValue(new ApiError(404, 'Not found'))

    const wrapper = await mountEditView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Customer not found.')
  })

  it('shows a required error for empty full name and does not call the API', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountEditView(makeRouter())
    await flushPromises()

    await wrapper.find('#customer-fullName').setValue('')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Full name is required.')
    expect(updateCustomerMock).not.toHaveBeenCalled()
  })

  it('shows an invalid-email error for a malformed email', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountEditView(makeRouter())
    await flushPromises()

    await wrapper.find('#customer-email').setValue('not-an-email')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Enter a valid email address.')
    expect(updateCustomerMock).not.toHaveBeenCalled()
  })

  it('disables Save while updating', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())
    let resolveUpdate!: (value: Customer) => void
    updateCustomerMock.mockReturnValue(
      new Promise((resolve) => {
        resolveUpdate = resolve
      }),
    )

    const wrapper = await mountEditView(makeRouter())
    await flushPromises()

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()

    resolveUpdate!(makeCustomer())
    await flushPromises()
  })

  it('navigates to the customers list on success', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())
    updateCustomerMock.mockResolvedValue(makeCustomer({ fullName: 'Updated Name' }))

    const router = makeRouter()
    const wrapper = await mountEditView(router)
    await flushPromises()

    await wrapper.find('#customer-fullName').setValue('Updated Name')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/customers')
  })

  it('shows the duplicate-email message on a 409 and stays on the form', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())
    updateCustomerMock.mockRejectedValue(new ApiError(409, 'A customer with this email already exists.'))

    const router = makeRouter()
    const wrapper = await mountEditView(router)
    await flushPromises()

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('A customer with this email already exists.')
    expect(router.currentRoute.value.path).toBe('/customers/1/edit')
  })

  it('navigates to the customers list on Cancel without calling the API', async () => {
    getCustomerMock.mockResolvedValue(makeCustomer())

    const router = makeRouter()
    const wrapper = await mountEditView(router)
    await flushPromises()

    await wrapper.find('button[type="button"]').trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/customers')
    expect(updateCustomerMock).not.toHaveBeenCalled()
  })
})
