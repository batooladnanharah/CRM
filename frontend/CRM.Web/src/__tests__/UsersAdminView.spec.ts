import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import UsersAdminView from '@/modules/security/views/UsersAdminView.vue'
import { i18n } from '@/i18n'
import { useAuthStore } from '@/stores/auth'
import type { AdminUserListItem } from '@/types/security'
import type { listCustomers } from '@/api/customers'
import type { Customer } from '@/types/customers'

const { listCustomersMock } = vi.hoisted(() => ({
  listCustomersMock: vi.fn<typeof listCustomers>(),
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

function makeUser(overrides: Partial<AdminUserListItem> = {}): AdminUserListItem {
  return {
    id: '1',
    email: 'active@crm.local',
    name: 'Active Agent',
    role: 'agent',
    isDisabled: false,
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    users: [] as AdminUserListItem[],
    usersPage: 1,
    usersPageSize: 25,
    usersTotalCount: 0,
    usersSearch: '',
    usersRoleFilter: '' as string,
    usersLoading: false,
    usersError: null as string | null,
    mutating: false,
    mutateError: null as string | null,
    fetchUsers: vi.fn<() => Promise<void>>(),
    setUsersSearch: vi.fn<(term: string) => void>(),
    setUsersFilters: vi.fn<(filters: Record<string, unknown>) => void>(),
    setUsersPage: vi.fn<(page: number) => void>(),
    changeRole: vi.fn<() => Promise<void>>(),
    disable: vi.fn<() => Promise<void>>(),
    enable: vi.fn<() => Promise<void>>(),
    create: vi.fn<() => Promise<void>>(),
    update: vi.fn<() => Promise<void>>(),
    roles: [],
    rolesLoading: false,
    rolesError: null as string | null,
    loadRoles: vi.fn<() => Promise<void>>(),
    permissionsFor: vi.fn<(role: string) => string[]>().mockReturnValue([]),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/security', () => ({
  useSecurityStore: () => fakeStore,
}))

function mountView() {
  return mount(UsersAdminView, { global: { plugins: [i18n] } })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
  listCustomersMock.mockReset()
  listCustomersMock.mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0 })
})

afterEach(() => {
  vi.useRealTimers()
})

describe('UsersAdminView', () => {
  it('calls fetchUsers on mount', () => {
    mountView()

    expect(fakeStore.fetchUsers).toHaveBeenCalledOnce()
  })

  it('renders a row per user', () => {
    fakeStore = makeFakeStore({ users: [makeUser({ name: 'Second Agent' })] })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('Second Agent')
  })

  it('shows the empty state when there are no users', () => {
    const wrapper = mountView()

    expect(wrapper.text()).toContain('No users found.')
  })

  it('calls setUsersSearch on search input', async () => {
    const wrapper = mountView()

    await wrapper.find('input[type="search"]').setValue('agent')

    expect(fakeStore.setUsersSearch).toHaveBeenCalledWith('agent')
  })

  it('calls setUsersFilters when the role filter changes', async () => {
    const wrapper = mountView()

    const selects = wrapper.findAll('select')
    await selects[0]!.setValue('admin')

    expect(fakeStore.setUsersFilters).toHaveBeenCalledWith({ role: 'admin' })
  })

  it('disables the role select and disable button for the current user row (self)', () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    fakeStore = makeFakeStore({ users: [makeUser({ id: '1', role: 'admin' })] })
    const wrapper = mountView()

    const roleSelect = wrapper.find('tbody select')
    const disableButton = wrapper.findAll('tbody button').find((b) => b.text() === 'Disable')!
    expect(roleSelect.attributes('disabled')).toBeDefined()
    expect(disableButton.attributes('disabled')).toBeDefined()
  })

  it('calls store.changeRole when a role select changes for another user', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: 'admin-1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    fakeStore = makeFakeStore({ users: [makeUser({ id: '2', role: 'agent' })] })
    const wrapper = mountView()

    await wrapper.find('tbody select').setValue('admin')

    expect(fakeStore.changeRole).toHaveBeenCalledWith('2', 'admin')
  })

  it('calls store.disable after confirmation for another user', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: 'admin-1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    fakeStore = makeFakeStore({ users: [makeUser({ id: '2' })] })
    const wrapper = mountView()

    const disableButton = wrapper.findAll('tbody button').find((b) => b.text() === 'Disable')!
    await disableButton.trigger('click')

    expect(fakeStore.disable).toHaveBeenCalledWith('2')
  })

  it('shows the self-mutation message when mutateError is cannot_modify_self', () => {
    fakeStore = makeFakeStore({ mutateError: 'cannot_modify_self' })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('You cannot change your own role or disable your own account.')
  })

  it('disables Deactivate on the current user row', () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    fakeStore = makeFakeStore({ users: [makeUser({ id: '1' })] })
    const wrapper = mountView()

    const disableButton = wrapper
      .findAll('tbody button')
      .find((b) => b.text() === 'Disable')!
    expect(disableButton.attributes('disabled')).toBeDefined()
  })

  it('shows a pluralized permissions summary and reveals the full list in a popover on click, without altering the summary', async () => {
    fakeStore = makeFakeStore({ users: [makeUser({ id: '1' })] })
    fakeStore.permissionsFor = vi
      .fn<(role: string) => string[]>()
      .mockReturnValue(['customers.manage', 'tickets.manage', 'tickets.escalate', 'kb.view', 'sla.manage'])
    const wrapper = mountView()

    expect(wrapper.find('.permissions-cell .permissions-text').text()).toBe('5 permissions')
    const toggle = wrapper.find('.permissions-toggle')
    expect(toggle.text()).toBe('Show all')
    expect(document.querySelector('.permissions-popover')).toBeNull()

    await toggle.trigger('click')

    expect(wrapper.find('.permissions-cell .permissions-text').text()).toBe('5 permissions')
    const popoverText = document.querySelector('.permissions-popover')?.textContent ?? ''
    expect(popoverText).toContain('Customers')
    expect(popoverText).toContain('Manage')
    expect(wrapper.find('.permissions-toggle').text()).toBe('Show less')

    await wrapper.find('.permissions-toggle').trigger('click')

    expect(document.querySelector('.permissions-popover')).toBeNull()
  })

  it('shows a singular permissions summary when the role has exactly 1 permission', () => {
    fakeStore = makeFakeStore({ users: [makeUser({ id: '1', role: 'customer' })] })
    fakeStore.permissionsFor = vi.fn<(role: string) => string[]>().mockReturnValue(['portal.access'])
    const wrapper = mountView()

    expect(wrapper.find('.permissions-cell .permissions-text').text()).toBe('1 permission')
  })
})

describe('UsersAdminView create/edit dialogs', () => {
  it('opens the create dialog and calls store.create with the form values', async () => {
    fakeStore = makeFakeStore()
    fakeStore.create = vi.fn<() => Promise<void>>().mockResolvedValue(undefined)
    const wrapper = mountView()

    await wrapper.find('.page-heading button').trigger('click')
    expect(wrapper.text()).toContain('Create User')

    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')
    const nameInputs = wrapper.findAll('input[type="text"]')
    await emailInput.setValue('new.agent@crm.local')
    await passwordInput.setValue('Correct#Passw0rd!')
    await nameInputs[nameInputs.length - 1]!.setValue('New Agent')

    await wrapper.find('.user-form').trigger('submit')
    await vi.waitFor(() => expect(fakeStore.create).toHaveBeenCalled())

    expect(fakeStore.create).toHaveBeenCalledWith({
      email: 'new.agent@crm.local',
      password: 'Correct#Passw0rd!',
      name: 'New Agent',
      role: 'agent',
    })
  })

  it('does not call store.create when the create form is invalid', async () => {
    fakeStore = makeFakeStore()
    const wrapper = mountView()

    await wrapper.find('.page-heading button').trigger('click')
    await wrapper.find('.user-form').trigger('submit')

    expect(fakeStore.create).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('valid email')
  })

  it('opens the edit dialog pre-filled and calls store.update', async () => {
    fakeStore = makeFakeStore({ users: [makeUser({ id: '2', email: 'old@crm.local', name: 'Old Name' })] })
    fakeStore.update = vi.fn<() => Promise<void>>().mockResolvedValue(undefined)
    const wrapper = mountView()

    const editButton = wrapper.findAll('tbody button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')

    const emailInput = wrapper.find('input[type="email"]')
    expect((emailInput.element as HTMLInputElement).value).toBe('old@crm.local')

    await emailInput.setValue('renamed@crm.local')
    await wrapper.find('.user-form').trigger('submit')
    await vi.waitFor(() => expect(fakeStore.update).toHaveBeenCalled())

    expect(fakeStore.update).toHaveBeenCalledWith('2', { email: 'renamed@crm.local', name: 'Old Name' })
  })

  it('displays the mapped error when store.create fails', async () => {
    fakeStore = makeFakeStore()
    fakeStore.create = vi.fn<() => Promise<void>>().mockRejectedValue(new Error('failed'))
    fakeStore.mutateError = 'duplicate_email'
    const wrapper = mountView()

    await wrapper.find('.page-heading button').trigger('click')
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')
    const nameInputs = wrapper.findAll('input[type="text"]')
    await emailInput.setValue('dup@crm.local')
    await passwordInput.setValue('Correct#Passw0rd!')
    await nameInputs[nameInputs.length - 1]!.setValue('Dup')

    await wrapper.find('.user-form').trigger('submit')
    await vi.waitFor(() => expect(fakeStore.create).toHaveBeenCalled())

    expect(wrapper.text()).toContain('already exists')
  })

  it('shows a required customer picker when the create role is set to customer', async () => {
    fakeStore = makeFakeStore()
    const wrapper = mountView()

    await wrapper.find('.page-heading button').trigger('click')
    expect(wrapper.find('#create-user-customer').exists()).toBe(false)

    const roleSelect = wrapper.find('#create-user-role')
    await roleSelect.setValue('customer')

    expect(wrapper.find('#create-user-customer').exists()).toBe(true)
  })

  it('does not call store.create for a customer-role user with no customer selected', async () => {
    fakeStore = makeFakeStore()
    const wrapper = mountView()

    await wrapper.find('.page-heading button').trigger('click')
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')
    const nameInputs = wrapper.findAll('input[type="text"]')
    await emailInput.setValue('newcustomer@example.com')
    await passwordInput.setValue('Correct#Passw0rd!')
    await nameInputs[nameInputs.length - 1]!.setValue('New Customer')
    await wrapper.find('#create-user-role').setValue('customer')

    await wrapper.find('.user-form').trigger('submit')

    expect(fakeStore.create).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('linking a customer record')
  })

  it('calls store.create with the selected customerId once a customer is picked', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] })
    listCustomersMock.mockResolvedValue({
      items: [makeCustomer({ id: 'customer-9', fullName: 'Bob Martinez' })],
      page: 1,
      pageSize: 10,
      totalCount: 1,
    })
    fakeStore = makeFakeStore()
    fakeStore.create = vi.fn<() => Promise<void>>().mockResolvedValue(undefined)
    const wrapper = mountView()

    await wrapper.find('.page-heading button').trigger('click')
    const emailInput = wrapper.find('input[type="email"]')
    const passwordInput = wrapper.find('input[type="password"]')
    const nameInputs = wrapper.findAll('input[type="text"]')
    await emailInput.setValue('newcustomer@example.com')
    await passwordInput.setValue('Correct#Passw0rd!')
    await nameInputs[nameInputs.length - 1]!.setValue('New Customer')
    await wrapper.find('#create-user-role').setValue('customer')

    await wrapper.find('#create-user-customer').setValue('Bob')
    vi.advanceTimersByTime(300)
    await flushPromises()

    const suggestion = wrapper.find('.customer-suggestions li')
    expect(suggestion.exists()).toBe(true)
    await suggestion.trigger('click')

    vi.useRealTimers()
    await wrapper.find('.user-form').trigger('submit')
    await vi.waitFor(() => expect(fakeStore.create).toHaveBeenCalled())

    expect(fakeStore.create).toHaveBeenCalledWith(
      expect.objectContaining({ role: 'customer', customerId: 'customer-9' }),
    )
  })
})
