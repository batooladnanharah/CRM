import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import type { logoutRequest } from '@/api/auth'

const { logoutRequestMock } = vi.hoisted(() => ({
  logoutRequestMock: vi.fn<typeof logoutRequest>(),
}))

vi.mock('@/api/auth', async () => {
  const actual = await vi.importActual<typeof import('@/api/auth')>('@/api/auth')
  return {
    ...actual,
    logoutRequest: logoutRequestMock,
  }
})

const TOKEN_KEY = 'crm.auth.token'
const USER_KEY = 'crm.auth.user'

beforeEach(() => {
  setActivePinia(createPinia())
  sessionStorage.clear()
  logoutRequestMock.mockReset()
})

describe('auth store', () => {
  it('hydrate() restores the token and user from sessionStorage', () => {
    sessionStorage.setItem(TOKEN_KEY, 'stored-token')
    sessionStorage.setItem(
      USER_KEY,
      JSON.stringify({ id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }),
    )

    const store = useAuthStore()
    store.hydrate()

    expect(store.token).toBe('stored-token')
    expect(store.user?.email).toBe('agent@crm.local')
    expect(store.isAuthenticated).toBe(true)
  })

  it('logout() clears both in-memory state and sessionStorage', async () => {
    logoutRequestMock.mockResolvedValue(undefined)
    sessionStorage.setItem(TOKEN_KEY, 'stored-token')
    sessionStorage.setItem(
      USER_KEY,
      JSON.stringify({ id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }),
    )

    const store = useAuthStore()
    store.hydrate()
    await store.logout()

    expect(store.token).toBeNull()
    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem(TOKEN_KEY)).toBeNull()
    expect(sessionStorage.getItem(USER_KEY)).toBeNull()
    expect(logoutRequestMock).toHaveBeenCalledOnce()
  })

  it('logout() still clears state when the API call fails', async () => {
    logoutRequestMock.mockRejectedValue(new Error('network down'))
    sessionStorage.setItem(TOKEN_KEY, 'stored-token')
    sessionStorage.setItem(
      USER_KEY,
      JSON.stringify({ id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }),
    )

    const store = useAuthStore()
    store.hydrate()

    await expect(store.logout()).resolves.toBeUndefined()

    expect(store.token).toBeNull()
    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem(TOKEN_KEY)).toBeNull()
    expect(sessionStorage.getItem(USER_KEY)).toBeNull()
  })

  it('logout() called while unauthenticated is a safe no-op', async () => {
    logoutRequestMock.mockRejectedValue(new Error('401'))

    const store = useAuthStore()

    await expect(store.logout()).resolves.toBeUndefined()

    expect(store.token).toBeNull()
    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
  })

  it('hasRole()/isAdmin/isAgent/isCustomer reflect the roles on the hydrated user', () => {
    sessionStorage.setItem(TOKEN_KEY, 'stored-token')
    sessionStorage.setItem(
      USER_KEY,
      JSON.stringify({ id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }),
    )

    const store = useAuthStore()
    store.hydrate()

    expect(store.hasRole('agent')).toBe(true)
    expect(store.hasRole('admin')).toBe(false)
    expect(store.isAgent).toBe(true)
    expect(store.isAdmin).toBe(false)
    expect(store.isCustomer).toBe(false)
  })

  it('hasRole()/isAdmin/isAgent satisfy a multi-role user', () => {
    sessionStorage.setItem(TOKEN_KEY, 'stored-token')
    sessionStorage.setItem(
      USER_KEY,
      JSON.stringify({
        id: '1',
        name: 'Admin Agent',
        email: 'admin-agent@crm.local',
        roles: ['admin', 'agent'],
      }),
    )

    const store = useAuthStore()
    store.hydrate()

    expect(store.isAdmin).toBe(true)
    expect(store.isAgent).toBe(true)
    expect(store.isCustomer).toBe(false)
  })

  it('hasRole()/isAdmin return false when unauthenticated', () => {
    const store = useAuthStore()

    expect(store.hasRole('admin')).toBe(false)
    expect(store.isAdmin).toBe(false)
    expect(store.isAgent).toBe(false)
    expect(store.isCustomer).toBe(false)
  })
})
