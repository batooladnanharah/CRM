import { ref } from 'vue'
import { defineStore } from 'pinia'
import { createCustomer, getCustomer, listCustomers, updateCustomer } from '@/api/customers'
import { ApiError } from '@/api/http'
import { i18n } from '@/i18n'
import { useToast } from '@/composables/useToast'
import type {
  Customer,
  CreateCustomerPayload,
  CustomerListQuery,
  UpdateCustomerPayload,
} from '@/types/customers'

const SEARCH_DEBOUNCE_MS = 300
const t = i18n.global.t

export const useCustomersStore = defineStore('customers', () => {
  const items = ref<Customer[]>([])
  const page = ref(1)
  const pageSize = ref(25)
  const totalCount = ref(0)
  const search = ref('')
  const company = ref('')
  const sortBy = ref<CustomerListQuery['sortBy']>('createdAtUtc')
  const sortDir = ref<CustomerListQuery['sortDir']>('asc')
  const loading = ref(false)
  const error = ref<string | null>(null)
  const creating = ref(false)
  const createError = ref<string | null>(null)
  const current = ref<Customer | null>(null)
  const loadingCurrent = ref(false)
  const notFound = ref(false)
  const loadError = ref<string | null>(null)
  const updating = ref(false)
  const updateError = ref<string | null>(null)

  let searchDebounceHandle: ReturnType<typeof setTimeout> | null = null
  let requestSequence = 0

  async function fetch(overrides?: Partial<CustomerListQuery>) {
    if (overrides?.page !== undefined) page.value = overrides.page
    if (overrides?.pageSize !== undefined) pageSize.value = overrides.pageSize
    if (overrides?.search !== undefined) search.value = overrides.search
    if (overrides?.company !== undefined) company.value = overrides.company
    if (overrides?.sortBy !== undefined) sortBy.value = overrides.sortBy
    if (overrides?.sortDir !== undefined) sortDir.value = overrides.sortDir

    loading.value = true
    error.value = null
    items.value = []

    const sequence = ++requestSequence

    try {
      const result = await listCustomers({
        search: search.value || undefined,
        company: company.value || undefined,
        sortBy: sortBy.value,
        sortDir: sortDir.value,
        page: page.value,
        pageSize: pageSize.value,
      })

      // Ignore stale responses from an earlier, now-superseded request so
      // rapid search/sort/pagination clicks don't cause results to flicker.
      if (sequence !== requestSequence) {
        return
      }

      items.value = result.items
      page.value = result.page
      pageSize.value = result.pageSize
      totalCount.value = result.totalCount
      loading.value = false
    } catch {
      if (sequence !== requestSequence) {
        return
      }

      error.value = 'errorLoad'
      loading.value = false
    }
  }

  function setSearch(term: string) {
    search.value = term

    if (searchDebounceHandle) {
      clearTimeout(searchDebounceHandle)
    }

    searchDebounceHandle = setTimeout(() => {
      searchDebounceHandle = null
      void fetch({ page: 1 })
    }, SEARCH_DEBOUNCE_MS)
  }

  function setCompany(value: string) {
    company.value = value
    void fetch({ page: 1 })
  }

  function reset() {
    if (searchDebounceHandle) {
      clearTimeout(searchDebounceHandle)
      searchDebounceHandle = null
    }
    search.value = ''
    company.value = ''
    void fetch({ page: 1 })
  }

  function setSort(column: NonNullable<CustomerListQuery['sortBy']>) {
    if (sortBy.value === column) {
      sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortBy.value = column
      sortDir.value = 'asc'
    }

    void fetch({ page: 1 })
  }

  function setPage(nextPage: number) {
    void fetch({ page: nextPage })
  }

  async function create(payload: CreateCustomerPayload): Promise<Customer> {
    creating.value = true
    createError.value = null

    try {
      const customer = await createCustomer(payload)
      return customer
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        createError.value = 'duplicateEmail'
      } else {
        createError.value = 'generic'
      }
      throw error
    } finally {
      creating.value = false
    }
  }

  async function getById(id: string) {
    loadingCurrent.value = true
    notFound.value = false
    loadError.value = null
    current.value = null

    try {
      current.value = await getCustomer(id)
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        notFound.value = true
      } else {
        loadError.value = 'errorLoad'
      }
    } finally {
      loadingCurrent.value = false
    }
  }

  function clearCurrent() {
    current.value = null
    loadingCurrent.value = false
    notFound.value = false
    loadError.value = null
  }

  async function update(id: string, payload: UpdateCustomerPayload): Promise<Customer> {
    updating.value = true
    updateError.value = null

    try {
      const customer = await updateCustomer(id, payload)
      current.value = customer
      useToast().success(t('notifications.customers.updated'))
      return customer
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        updateError.value = 'duplicateEmail'
      } else {
        updateError.value = 'generic'
      }
      throw error
    } finally {
      updating.value = false
    }
  }

  return {
    items,
    page,
    pageSize,
    totalCount,
    search,
    company,
    sortBy,
    sortDir,
    loading,
    error,
    creating,
    createError,
    current,
    loadingCurrent,
    notFound,
    loadError,
    updating,
    updateError,
    fetch,
    setSearch,
    setCompany,
    reset,
    setSort,
    setPage,
    create,
    getById,
    update,
    clearCurrent,
  }
})
