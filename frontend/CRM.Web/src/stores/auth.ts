import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { loginRequest, logoutRequest } from '@/api/auth'
import { ApiError, NetworkError } from '@/api/http'
import { Roles, type AuthUser } from '@/types/auth'

const TOKEN_STORAGE_KEY = 'crm.auth.token'
const USER_STORAGE_KEY = 'crm.auth.user'

type AuthStatus = 'idle' | 'loading' | 'error'

// The token/user are persisted in sessionStorage rather than localStorage.
// sessionStorage avoids long-lived XSS exposure (cleared when the tab closes)
// but does not survive tab close or cross-tab sharing — each browser tab has
// its own independent session. A follow-up story may replace this with an
// HTTP-only cookie once the backend supports it.
export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const token = ref<string | null>(null)
  const status = ref<AuthStatus>('idle')
  const errorMessage = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value)

  function hasRole(role: string): boolean {
    return user.value?.roles.includes(role) ?? false
  }

  const isAdmin = computed(() => hasRole(Roles.Admin))
  const isAgent = computed(() => hasRole(Roles.Agent))
  const isCustomer = computed(() => hasRole(Roles.Customer))

  // Backend is authoritative — this only drives UI visibility (nav items,
  // buttons, route guards). Every protected call is re-checked server-side
  // regardless of what this reports.
  const permissions = computed(() => new Set(user.value?.permissions ?? []))

  function can(permission: string): boolean {
    return permissions.value.has(permission)
  }

  function persist() {
    try {
      if (token.value) {
        sessionStorage.setItem(TOKEN_STORAGE_KEY, token.value)
      } else {
        sessionStorage.removeItem(TOKEN_STORAGE_KEY)
      }
      if (user.value) {
        sessionStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user.value))
      } else {
        sessionStorage.removeItem(USER_STORAGE_KEY)
      }
    } catch {
      // sessionStorage may be unavailable (private browsing, storage blocked);
      // the in-memory state still works for the current page lifetime.
    }
  }

  async function login(email: string, password: string) {
    if (status.value === 'loading') {
      return
    }

    status.value = 'loading'
    errorMessage.value = null

    try {
      const response = await loginRequest(email, password)
      user.value = response.user
      token.value = response.token
      status.value = 'idle'
      persist()
    } catch (error) {
      user.value = null
      token.value = null
      persist()

      if (error instanceof NetworkError) {
        errorMessage.value = 'network'
      } else if (error instanceof ApiError) {
        errorMessage.value = 'invalidCredentials'
      } else {
        errorMessage.value = 'invalidCredentials'
      }
      status.value = 'error'
      throw error
    }
  }

  async function logout() {
    // The backend logout call is best-effort: JWTs are stateless, so there is
    // nothing to revoke server-side. Client state must be cleared even if the
    // request fails (network offline, backend down, already-expired token).
    try {
      await logoutRequest()
    } catch (error) {
      console.warn('Logout request failed; clearing local session anyway.', error)
    }

    user.value = null
    token.value = null
    status.value = 'idle'
    errorMessage.value = null
    persist()
  }

  function hydrate() {
    try {
      const storedToken = sessionStorage.getItem(TOKEN_STORAGE_KEY)
      const storedUser = sessionStorage.getItem(USER_STORAGE_KEY)
      if (storedToken && storedUser) {
        token.value = storedToken
        user.value = JSON.parse(storedUser) as AuthUser
      }
    } catch {
      // Corrupt or inaccessible storage — start unauthenticated.
    }
  }

  return {
    user,
    token,
    status,
    errorMessage,
    isAuthenticated,
    isAdmin,
    isAgent,
    isCustomer,
    permissions,
    can,
    hasRole,
    login,
    logout,
    hydrate,
  }
})
