import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useSecurityStore } from '@/stores/security'
import { ApiError } from '@/api/http'
import type {
  assignRole,
  createUser,
  disableUser,
  enableUser,
  listAuditLog,
  listUsers,
  updateUser,
} from '@/api/security'
import type { AdminUserListItem, AuditLogEntry } from '@/types/security'

const {
  listUsersMock,
  assignRoleMock,
  disableUserMock,
  enableUserMock,
  listAuditLogMock,
  createUserMock,
  updateUserMock,
} = vi.hoisted(() => ({
  listUsersMock: vi.fn<typeof listUsers>(),
  assignRoleMock: vi.fn<typeof assignRole>(),
  disableUserMock: vi.fn<typeof disableUser>(),
  enableUserMock: vi.fn<typeof enableUser>(),
  listAuditLogMock: vi.fn<typeof listAuditLog>(),
  createUserMock: vi.fn<typeof createUser>(),
  updateUserMock: vi.fn<typeof updateUser>(),
}))

vi.mock('@/api/security', () => ({
  listUsers: listUsersMock,
  assignRole: assignRoleMock,
  disableUser: disableUserMock,
  enableUser: enableUserMock,
  listAuditLog: listAuditLogMock,
  createUser: createUserMock,
  updateUser: updateUserMock,
}))

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

function makeAuditEntry(overrides: Partial<AuditLogEntry> = {}): AuditLogEntry {
  return {
    id: 'a1',
    occurredAtUtc: '2026-01-01T00:00:00Z',
    actorUserId: 'admin-1',
    actorEmail: 'admin@crm.local',
    action: 'user.role.assigned',
    targetType: 'user',
    targetId: '1',
    ipAddress: '127.0.0.1',
    payloadJson: '{"before":["agent"],"after":["admin"]}',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listUsersMock.mockReset()
  assignRoleMock.mockReset()
  disableUserMock.mockReset()
  enableUserMock.mockReset()
  listAuditLogMock.mockReset()
  createUserMock.mockReset()
  updateUserMock.mockReset()
})

describe('security store', () => {
  it('has the expected initial state', () => {
    const store = useSecurityStore()

    expect(store.users).toEqual([])
    expect(store.usersLoading).toBe(false)
    expect(store.usersError).toBeNull()
    expect(store.auditEntries).toEqual([])
    expect(store.auditLoading).toBe(false)
    expect(store.auditError).toBeNull()
  })

  it('fetchUsers() populates users and pagination on success', async () => {
    const user = makeUser()
    listUsersMock.mockResolvedValue({ items: [user], page: 1, pageSize: 25, totalCount: 1 })

    const store = useSecurityStore()
    await store.fetchUsers()

    expect(store.users).toEqual([user])
    expect(store.usersTotalCount).toBe(1)
    expect(store.usersError).toBeNull()
  })

  it('fetchUsers() sets errorLoad and does not throw on failure', async () => {
    listUsersMock.mockRejectedValue(new Error('network down'))

    const store = useSecurityStore()
    await expect(store.fetchUsers()).resolves.toBeUndefined()

    expect(store.usersError).toBe('errorLoad')
    expect(store.users).toEqual([])
  })

  it('setUsersFilters triggers an immediate refetch with the new role/disabled filters', async () => {
    listUsersMock.mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0 })

    const store = useSecurityStore()
    store.setUsersFilters({ role: 'admin', disabled: true })

    await vi.waitFor(() => expect(listUsersMock).toHaveBeenCalledTimes(1))
    expect(listUsersMock).toHaveBeenCalledWith(
      expect.objectContaining({ role: 'admin', disabled: true, page: 1 }),
    )
  })

  it('create() re-fetches the current page on success', async () => {
    listUsersMock.mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0 })
    createUserMock.mockResolvedValue({
      id: 'new-1', email: 'new.agent@crm.local', name: 'New Agent', role: 'agent', isDisabled: false,
      customerId: null, createdAtUtc: '2026-01-01T00:00:00Z',
    })

    const store = useSecurityStore()
    const result = await store.create({
      email: 'new.agent@crm.local', password: 'Correct#Passw0rd!', name: 'New Agent', role: 'agent',
    })

    expect(result.email).toBe('new.agent@crm.local')
    expect(listUsersMock).toHaveBeenCalledTimes(1)
    expect(store.mutating).toBe(false)
    expect(store.mutateError).toBeNull()
  })

  it('create() sets mutateError to the conflict code and rethrows on 409', async () => {
    createUserMock.mockRejectedValue(new ApiError(409, 'duplicate_email'))

    const store = useSecurityStore()
    await expect(
      store.create({ email: 'dup@crm.local', password: 'Correct#Passw0rd!', name: 'Dup', role: 'agent' }),
    ).rejects.toThrow('duplicate_email')

    expect(store.mutateError).toBe('duplicate_email')
    expect(store.mutating).toBe(false)
  })

  it('update() replaces the user email/name in place on success', async () => {
    listUsersMock.mockResolvedValue({
      items: [makeUser({ id: '1', email: 'old@crm.local', name: 'Old Name' })],
      page: 1, pageSize: 25, totalCount: 1,
    })
    const store = useSecurityStore()
    await store.fetchUsers()

    updateUserMock.mockResolvedValue({
      id: '1', email: 'new@crm.local', name: 'New Name', role: 'agent', isDisabled: false,
      customerId: null, createdAtUtc: '2026-01-01T00:00:00Z',
    })
    await store.update('1', { email: 'new@crm.local', name: 'New Name' })

    expect(store.users[0]!.email).toBe('new@crm.local')
    expect(store.users[0]!.name).toBe('New Name')
  })

  it('update() sets mutateError to the conflict code and rethrows on 409', async () => {
    updateUserMock.mockRejectedValue(new ApiError(409, 'duplicate_email'))

    const store = useSecurityStore()
    await expect(store.update('1', { email: 'dup@crm.local', name: 'Dup' })).rejects.toThrow('duplicate_email')

    expect(store.mutateError).toBe('duplicate_email')
  })

  it('changeRole() updates the user role in place on success', async () => {
    listUsersMock.mockResolvedValue({
      items: [makeUser({ id: '1', role: 'agent' })], page: 1, pageSize: 25, totalCount: 1,
    })
    const store = useSecurityStore()
    await store.fetchUsers()

    assignRoleMock.mockResolvedValue({
      id: '1', email: 'active@crm.local', name: 'Active Agent', role: 'admin', isDisabled: false,
      customerId: null, createdAtUtc: '2026-01-01T00:00:00Z',
    })
    await store.changeRole('1', 'admin')

    expect(store.users[0]!.role).toBe('admin')
  })

  it('changeRole() sets mutateError to the conflict code and rethrows on 409', async () => {
    assignRoleMock.mockRejectedValue(new ApiError(409, 'cannot_modify_self'))

    const store = useSecurityStore()
    await expect(store.changeRole('1', 'admin')).rejects.toThrow('cannot_modify_self')

    expect(store.mutateError).toBe('cannot_modify_self')
  })

  it('disable() marks the user disabled in place on success', async () => {
    listUsersMock.mockResolvedValue({
      items: [makeUser({ id: '1', isDisabled: false })], page: 1, pageSize: 25, totalCount: 1,
    })
    const store = useSecurityStore()
    await store.fetchUsers()

    disableUserMock.mockResolvedValue({
      id: '1', email: 'active@crm.local', name: 'Active Agent', role: 'agent', isDisabled: true,
      customerId: null, createdAtUtc: '2026-01-01T00:00:00Z',
    })
    await store.disable('1')

    expect(store.users[0]!.isDisabled).toBe(true)
  })

  it('enable() marks the user enabled in place on success', async () => {
    listUsersMock.mockResolvedValue({
      items: [makeUser({ id: '1', isDisabled: true })], page: 1, pageSize: 25, totalCount: 1,
    })
    const store = useSecurityStore()
    await store.fetchUsers()

    enableUserMock.mockResolvedValue({
      id: '1', email: 'active@crm.local', name: 'Active Agent', role: 'agent', isDisabled: false,
      customerId: null, createdAtUtc: '2026-01-01T00:00:00Z',
    })
    await store.enable('1')

    expect(store.users[0]!.isDisabled).toBe(false)
  })

  it('fetchAuditLog() populates auditEntries and pagination on success', async () => {
    const entry = makeAuditEntry()
    listAuditLogMock.mockResolvedValue({ items: [entry], page: 1, pageSize: 25, totalCount: 1 })

    const store = useSecurityStore()
    await store.fetchAuditLog()

    expect(store.auditEntries).toEqual([entry])
    expect(store.auditTotalCount).toBe(1)
  })

  it('fetchAuditLog() sets errorLoad and does not throw on failure', async () => {
    listAuditLogMock.mockRejectedValue(new Error('network down'))

    const store = useSecurityStore()
    await expect(store.fetchAuditLog()).resolves.toBeUndefined()

    expect(store.auditError).toBe('errorLoad')
    expect(store.auditEntries).toEqual([])
  })

  it('setAuditPage refetches with the requested page and filters', async () => {
    listAuditLogMock.mockResolvedValue({ items: [], page: 2, pageSize: 25, totalCount: 0 })

    const store = useSecurityStore()
    store.setAuditPage(2, { action: 'user.login.failed' })

    await vi.waitFor(() => expect(listAuditLogMock).toHaveBeenCalledTimes(1))
    expect(listAuditLogMock).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2, action: 'user.login.failed' }),
    )
  })
})
