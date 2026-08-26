import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import CustomersListView from '@/modules/customers/views/CustomersListView.vue'
import { i18n } from '@/i18n'
import type { Customer, CustomerListQuery } from '@/types/customers'

const mountOptions = {
  global: {
    plugins: [i18n],
    stubs: { RouterLink: true },
  },
}

const routeState = { query: {} as Record<string, string> }
const routerMock = { replace: vi.fn<(location: unknown) => void>() }

vi.mock('vue-router', () => ({
  useRoute: () => routeState,
  useRouter: () => routerMock,
}))

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: '1',
    fullName: 'Alice Johnson',
    email: 'alice@example.com',
    phone: '+1-555-0101',
    company: 'Acme Corp',
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    items: [] as Customer[],
    page: 1,
    pageSize: 25,
    totalCount: 0,
    search: '',
    company: '',
    sortBy: 'createdAtUtc' as const,
    sortDir: 'asc' as const,
    loading: false,
    error: null as string | null,
    fetch: vi.fn<() => Promise<void>>(),
    setSearch: vi.fn<(term: string) => void>(),
    setCompany: vi.fn<(company: string) => void>(),
    setSort: vi.fn<(column: NonNullable<CustomerListQuery['sortBy']>) => void>(),
    setPage: vi.fn<(page: number) => void>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/customers', () => ({
  useCustomersStore: () => fakeStore,
}))

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
  routeState.query = {}
  routerMock.replace.mockReset()
})

describe('CustomersListView', () => {
  it('renders rows from the store', () => {
    fakeStore.items = [makeCustomer()]
    fakeStore.totalCount = 1

    const wrapper = mount(CustomersListView, mountOptions)

    expect(wrapper.text()).toContain('Alice Johnson')
    expect(wrapper.text()).toContain('alice@example.com')
    expect(wrapper.text()).toContain('Acme Corp')
  })

  it('shows the empty state when there are no items', () => {
    const wrapper = mount(CustomersListView, mountOptions)

    expect(wrapper.text()).toContain('No customers found.')
  })

  it('shows the loading state', () => {
    fakeStore.loading = true

    const wrapper = mount(CustomersListView, mountOptions)

    expect(wrapper.text()).toContain('Loading customers…')
  })

  it('shows the error state', () => {
    fakeStore.error = 'errorLoad'

    const wrapper = mount(CustomersListView, mountOptions)

    expect(wrapper.text()).toContain('Could not load customers. Please try again.')
  })

  it('invokes store.setSearch when the search input changes', async () => {
    const wrapper = mount(CustomersListView, mountOptions)

    await wrapper.find('input[type="search"]').setValue('ali')

    expect(fakeStore.setSearch).toHaveBeenCalledWith('ali')
  })

  it('invokes store.setCompany when the company filter changes', async () => {
    fakeStore.items = [makeCustomer()]
    const wrapper = mount(CustomersListView, mountOptions)

    await wrapper.find('select').setValue('Acme Corp')

    expect(fakeStore.setCompany).toHaveBeenCalledWith('Acme Corp')
  })

  it('shows the filtered empty state', () => {
    fakeStore.search = 'missing'

    const wrapper = mount(CustomersListView, mountOptions)

    expect(wrapper.text()).toContain('No customers match your filters.')
  })

  it('hydrates search and company from the route query', () => {
    routeState.query = { search: 'alice', company: 'Acme Corp' }

    mount(CustomersListView, mountOptions)

    expect(fakeStore.search).toBe('alice')
    expect(fakeStore.company).toBe('Acme Corp')
    expect(fakeStore.fetch).toHaveBeenCalled()
  })

  it('invokes store.setSort when a column header is clicked', async () => {
    fakeStore.items = [makeCustomer()]

    const wrapper = mount(CustomersListView, mountOptions)

    await wrapper.findAll('th button')[0]!.trigger('click')

    expect(fakeStore.setSort).toHaveBeenCalledWith('fullName')
  })

  it('invokes store.setPage when Next is clicked', async () => {
    fakeStore.items = [makeCustomer()]
    fakeStore.totalCount = 50
    fakeStore.page = 1
    fakeStore.pageSize = 25

    const wrapper = mount(CustomersListView, mountOptions)

    const buttons = wrapper.findAll('button')
    const nextButton = buttons.find((b) => b.text() === 'Next')!
    await nextButton.trigger('click')

    expect(fakeStore.setPage).toHaveBeenCalledWith(2)
  })

  it('renders a View link for each customer row (visible even without admin/agent role)', () => {
    fakeStore.items = [makeCustomer()]

    const wrapper = mount(CustomersListView, mountOptions)

    // Only the unconditional "View" link renders here: "Add customer" and "Edit"
    // are both gated on authStore.isAdmin/isAgent, which default to false when
    // no auth token is set up in this test.
    expect(wrapper.findAll('router-link-stub')).toHaveLength(1)
  })

  it('does not call setPage from Prev when already on page 1', async () => {
    fakeStore.items = [makeCustomer()]
    fakeStore.page = 1

    const wrapper = mount(CustomersListView, mountOptions)

    const buttons = wrapper.findAll('button')
    const prevButton = buttons.find((b) => b.text() === 'Previous')!
    expect(prevButton.attributes('disabled')).toBeDefined()
  })
})
