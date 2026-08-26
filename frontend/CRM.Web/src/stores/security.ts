import { ref } from 'vue'
import { defineStore } from 'pinia'
import { assignRole, disableUser, enableUser, listAuditLog, listUsers } from '@/api/security'
import { ApiError } from '@/api/http'
import type { AdminRole, AdminUserListItem, AuditLogEntry, AuditLogQuery } from '@/types/security'

const SEARCH_DEBOUNCE_MS = 300

export const useSecurityStore = defineStore('security', () => {
  const users = ref<AdminUserListItem[]>([])
  const usersPage = ref(1)
  const usersPageSize = ref(25)
  const usersTotalCount = ref(0)
  const usersSearch = ref('')
  const usersRoleFilter = ref<AdminRole | ''>('')
  const usersDisabledFilter = ref<boolean | undefined>(undefined)
  const usersLoading = ref(false)
  const usersError = ref<string | null>(null)
  const mutating = ref(false)
  const mutateError = ref<string | null>(null)

  const auditEntries = ref<AuditLogEntry[]>([])
  const auditPage = ref(1)
  const auditPageSize = ref(25)
  const auditTotalCount = ref(0)
  const auditLoading = ref(false)
  const auditError = ref<string | null>(null)

  let searchDebounceHandle: ReturnType<typeof setTimeout> | null = null

  async function fetchUsers(overrides?: { page?: number }) {
    if (overrides?.page !== undefined) usersPage.value = overrides.page

    usersLoading.value = true
    usersError.value = null

    try {
      const result = await listUsers({
        search: usersSearch.value || undefined,
        role: usersRoleFilter.value || undefined,
        disabled: usersDisabledFilter.value,
        page: usersPage.value,
        pageSize: usersPageSize.value,
      })
      users.value = result.items
      usersPage.value = result.page
      usersPageSize.value = result.pageSize
      usersTotalCount.value = result.totalCount
    } catch {
      usersError.value = 'errorLoad'
    } finally {
      usersLoading.value = false
    }
  }

  function setUsersSearch(term: string) {
    usersSearch.value = term

    if (searchDebounceHandle) {
      clearTimeout(searchDebounceHandle)
    }
    searchDebounceHandle = setTimeout(() => {
      searchDebounceHandle = null
      void fetchUsers({ page: 1 })
    }, SEARCH_DEBOUNCE_MS)
  }

  function setUsersFilters(filters: { role?: AdminRole | ''; disabled?: boolean | undefined }) {
    if (filters.role !== undefined) usersRoleFilter.value = filters.role
    if (filters.disabled !== undefined) usersDisabledFilter.value = filters.disabled
    void fetchUsers({ page: 1 })
  }

  function setUsersPage(page: number) {
    void fetchUsers({ page })
  }

  async function changeRole(id: string, role: string) {
    mutating.value = true
    mutateError.value = null

    try {
      const updated = await assignRole(id, role)
      users.value = users.value.map((u) => (u.id === id ? { ...u, role: updated.role } : u))
      return updated
    } catch (err) {
      mutateError.value = errorCode(err)
      throw err
    } finally {
      mutating.value = false
    }
  }

  async function disable(id: string) {
    mutating.value = true
    mutateError.value = null

    try {
      const updated = await disableUser(id)
      users.value = users.value.map((u) => (u.id === id ? { ...u, isDisabled: true } : u))
      return updated
    } catch (err) {
      mutateError.value = errorCode(err)
      throw err
    } finally {
      mutating.value = false
    }
  }

  async function enable(id: string) {
    mutating.value = true
    mutateError.value = null

    try {
      const updated = await enableUser(id)
      users.value = users.value.map((u) => (u.id === id ? { ...u, isDisabled: false } : u))
      return updated
    } catch (err) {
      mutateError.value = errorCode(err)
      throw err
    } finally {
      mutating.value = false
    }
  }

  function errorCode(err: unknown): string {
    if (err instanceof ApiError && err.status === 409) {
      return err.message
    }
    return 'generic'
  }

  async function fetchAuditLog(filters?: AuditLogQuery) {
    if (filters?.page !== undefined) auditPage.value = filters.page

    auditLoading.value = true
    auditError.value = null

    try {
      const result = await listAuditLog({
        actorId: filters?.actorId,
        targetId: filters?.targetId,
        action: filters?.action,
        from: filters?.from,
        to: filters?.to,
        page: auditPage.value,
        pageSize: auditPageSize.value,
      })
      auditEntries.value = result.items
      auditPage.value = result.page
      auditPageSize.value = result.pageSize
      auditTotalCount.value = result.totalCount
    } catch {
      auditError.value = 'errorLoad'
    } finally {
      auditLoading.value = false
    }
  }

  function setAuditPage(page: number, filters?: Omit<AuditLogQuery, 'page' | 'pageSize'>) {
    void fetchAuditLog({ ...filters, page })
  }

  return {
    users,
    usersPage,
    usersPageSize,
    usersTotalCount,
    usersSearch,
    usersRoleFilter,
    usersDisabledFilter,
    usersLoading,
    usersError,
    mutating,
    mutateError,
    fetchUsers,
    setUsersSearch,
    setUsersFilters,
    setUsersPage,
    changeRole,
    disable,
    enable,
    auditEntries,
    auditPage,
    auditPageSize,
    auditTotalCount,
    auditLoading,
    auditError,
    fetchAuditLog,
    setAuditPage,
  }
})
