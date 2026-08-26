import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import CustomerCreateView from '@/modules/customers/views/CustomerCreateView.vue'
import CustomersListView from '@/modules/customers/views/CustomersListView.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type { createCustomer } from '@/api/customers'
import type { Customer } from '@/types/customers'

const { createCustomerMock } = vi.hoisted(() => ({
  createCustomerMock: vi.fn<typeof createCustomer>(),
}))

vi.mock('@/api/customers', () => ({
  createCustomer: createCustomerMock,
  listCustomers: vi
    .fn<() => Promise<{ items: never[]; page: number; pageSize: number; totalCount: number }>>()
    .mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0 }),
}))

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: '1',
    fullName: 'New Customer',
    email: 'new@example.com',
    phone: null,
    company: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/customers', name: 'customers', component: CustomersListView },
      { path: '/customers/new', name: 'customer-create', component: CustomerCreateView },
    ],
  })
}

async function mountCreateView(router: Router) {
  router.push('/customers/new')
  await router.isReady()

  return mount(CustomerCreateView, {
    global: {
      plugins: [router, i18n],
    },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  createCustomerMock.mockReset()
})

describe('CustomerCreateView', () => {
  it('renders the full name, email, phone, company fields and submit/cancel buttons', async () => {
    const wrapper = await mountCreateView(makeRouter())

    expect(wrapper.find('#customer-fullName').exists()).toBe(true)
    expect(wrapper.find('#customer-email').exists()).toBe(true)
    expect(wrapper.find('#customer-phone').exists()).toBe(true)
    expect(wrapper.find('#customer-company').exists()).toBe(true)
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
    expect(wrapper.find('button[type="button"]').exists()).toBe(true)
  })

  it('shows a required error for empty full name and does not call the API', async () => {
    const wrapper = await mountCreateView(makeRouter())

    await wrapper.find('#customer-email').setValue('new@example.com')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Full name is required.')
    expect(createCustomerMock).not.toHaveBeenCalled()
  })

  it('shows an invalid-email error for a malformed email', async () => {
    const wrapper = await mountCreateView(makeRouter())

    await wrapper.find('#customer-fullName').setValue('Someone')
    await wrapper.find('#customer-email').setValue('not-an-email')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Enter a valid email address.')
    expect(createCustomerMock).not.toHaveBeenCalled()
  })

  it('calls createCustomer with the trimmed payload on valid submit', async () => {
    createCustomerMock.mockResolvedValue(makeCustomer())

    const wrapper = await mountCreateView(makeRouter())
    await wrapper.find('#customer-fullName').setValue('  New Customer  ')
    await wrapper.find('#customer-email').setValue('  new@example.com  ')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(createCustomerMock).toHaveBeenCalledWith({
      fullName: 'New Customer',
      email: 'new@example.com',
      phone: null,
      company: null,
    })
  })

  it('disables the submit button while creating', async () => {
    let resolveCreate!: (value: Customer) => void
    createCustomerMock.mockReturnValue(
      new Promise((resolve) => {
        resolveCreate = resolve
      }),
    )

    const wrapper = await mountCreateView(makeRouter())
    await wrapper.find('#customer-fullName').setValue('New Customer')
    await wrapper.find('#customer-email').setValue('new@example.com')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()

    resolveCreate!(makeCustomer())
    await flushPromises()
  })

  it('navigates to the customers list on success', async () => {
    createCustomerMock.mockResolvedValue(makeCustomer())

    const router = makeRouter()
    const wrapper = await mountCreateView(router)
    await wrapper.find('#customer-fullName').setValue('New Customer')
    await wrapper.find('#customer-email').setValue('new@example.com')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/customers')
  })

  it('shows the duplicate-email message on a 409 and stays on the form', async () => {
    createCustomerMock.mockRejectedValue(new ApiError(409, 'A customer with this email already exists.'))

    const router = makeRouter()
    const wrapper = await mountCreateView(router)
    await wrapper.find('#customer-fullName').setValue('New Customer')
    await wrapper.find('#customer-email').setValue('new@example.com')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('A customer with this email already exists.')
    expect(router.currentRoute.value.path).toBe('/customers/new')
  })

  it('navigates to the customers list on Cancel without calling the API', async () => {
    const router = makeRouter()
    const wrapper = await mountCreateView(router)

    await wrapper.find('button[type="button"]').trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/customers')
    expect(createCustomerMock).not.toHaveBeenCalled()
  })
})
