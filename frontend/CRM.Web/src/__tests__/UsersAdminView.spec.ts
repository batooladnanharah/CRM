import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import UsersAdminView from '@/modules/security/views/UsersAdminView.vue'
import { i18n } from '@/i18n'
import { useAuthStore } from '@/stores/auth'
import type { AdminUserListItem } from '@/types/security'

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
    const disableButton = wrapper.find('tbody button')
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

    await wrapper.find('tbody button').trigger('click')

    expect(fakeStore.disable).toHaveBeenCalledWith('2')
  })

  it('shows the self-mutation message when mutateError is cannot_modify_self', () => {
    fakeStore = makeFakeStore({ mutateError: 'cannot_modify_self' })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('You cannot change your own role or disable your own account.')
  })
})
