import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCustomersStore } from '@/stores/customers'
import { ApiError } from '@/api/http'
import type { createCustomer, getCustomer, listCustomers, updateCustomer } from '@/api/customers'
import type { Customer, PagedResult } from '@/types/customers'

const { listCustomersMock, createCustomerMock, getCustomerMock, updateCustomerMock } = vi.hoisted(() => ({
  listCustomersMock: vi.fn<typeof listCustomers>(),
  createCustomerMock: vi.fn<typeof createCustomer>(),
  getCustomerMock: vi.fn<typeof getCustomer>(),
  updateCustomerMock: vi.fn<typeof updateCustomer>(),
}))

vi.mock('@/api/customers', () => ({
  listCustomers: listCustomersMock,
  createCustomer: createCustomerMock,
  getCustomer: getCustomerMock,
  updateCustomer: updateCustomerMock,
}))

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: '1',
    fullName: 'Alice Johnson',
    email: 'alice@example.com',
    phone: null,
    company: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makePage(items: Customer[], overrides: Partial<PagedResult<Customer>> = {}): PagedResult<Customer> {
  return {
    items,
    page: 1,
    pageSize: 25,
    totalCount: items.length,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listCustomersMock.mockReset()
  createCustomerMock.mockReset()
  getCustomerMock.mockReset()
  updateCustomerMock.mockReset()
})

describe('customers store', () => {
  it('has the expected initial state', () => {
    const store = useCustomersStore()

    expect(store.items).toEqual([])
    expect(store.page).toBe(1)
    expect(store.pageSize).toBe(25)
    expect(store.totalCount).toBe(0)
    expect(store.search).toBe('')
    expect(store.company).toBe('')
    expect(store.sortBy).toBe('createdAtUtc')
    expect(store.sortDir).toBe('asc')
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() populates items and pagination state on success', async () => {
    const customer = makeCustomer()
    listCustomersMock.mockResolvedValue(makePage([customer]))

    const store = useCustomersStore()
    await store.fetch()

    expect(store.items).toEqual([customer])
    expect(store.totalCount).toBe(1)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() sets an error and does not throw when the API call fails', async () => {
    listCustomersMock.mockRejectedValue(new Error('network down'))

    const store = useCustomersStore()
    await expect(store.fetch()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.loading).toBe(false)
    expect(store.items).toEqual([])
  })

  it('reset clears the query and refetches', async () => {
    listCustomersMock.mockResolvedValue(makePage([]))

    const store = useCustomersStore()
    store.search = 'alice'
    store.company = 'Acme Corp'

    store.reset()
    await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalledTimes(1))

    expect(store.search).toBe('')
    expect(store.company).toBe('')
    expect(listCustomersMock).toHaveBeenCalledWith(expect.objectContaining({ search: undefined, company: undefined }))
  })

  it('ignores a stale response when a newer request finishes first', async () => {
    let resolveFirst!: (result: PagedResult<Customer>) => void
    let resolveSecond!: (result: PagedResult<Customer>) => void
    listCustomersMock
      .mockReturnValueOnce(new Promise((resolve) => { resolveFirst = resolve }))
      .mockReturnValueOnce(new Promise((resolve) => { resolveSecond = resolve }))

    const store = useCustomersStore()
    const first = store.fetch({ search: 'old' })
    const second = store.fetch({ search: 'new' })

    resolveSecond(makePage([makeCustomer({ fullName: 'New Result' })]))
    await second
    resolveFirst(makePage([makeCustomer({ fullName: 'Old Result' })]))
    await first

    expect(store.items[0]?.fullName).toBe('New Result')
  })

  describe('setSearch', () => {
    beforeEach(() => {
      vi.useFakeTimers()
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('debounces search updates before triggering fetch', async () => {
      listCustomersMock.mockResolvedValue(makePage([]))

      const store = useCustomersStore()
      store.setSearch('ali')

      expect(listCustomersMock).not.toHaveBeenCalled()

      await vi.advanceTimersByTimeAsync(300)

      expect(listCustomersMock).toHaveBeenCalledTimes(1)
      expect(listCustomersMock).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'ali', page: 1 }),
      )
    })

    it('only fetches once for rapid successive keystrokes', async () => {
      listCustomersMock.mockResolvedValue(makePage([]))

      const store = useCustomersStore()
      store.setSearch('a')
      store.setSearch('al')
      store.setSearch('ali')

      await vi.advanceTimersByTimeAsync(300)

      expect(listCustomersMock).toHaveBeenCalledTimes(1)
      expect(listCustomersMock).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'ali' }),
      )
    })
  })

  it('setSort toggles direction on repeated clicks of the same column', async () => {
    listCustomersMock.mockResolvedValue(makePage([]))

    const store = useCustomersStore()

    store.setSort('fullName')
    await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalledTimes(1))
    expect(store.sortBy).toBe('fullName')
    expect(store.sortDir).toBe('asc')

    store.setSort('fullName')
    await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalledTimes(2))
    expect(store.sortDir).toBe('desc')
  })

  it('setSort resets to ascending when switching to a different column', async () => {
    listCustomersMock.mockResolvedValue(makePage([]))

    const store = useCustomersStore()

    store.setSort('fullName')
    await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalledTimes(1))
    store.setSort('fullName')
    await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalledTimes(2))
    expect(store.sortDir).toBe('desc')

    store.setSort('email')
    await vi.waitFor(() => expect(listCustomersMock).toHaveBeenCalledTimes(3))
    expect(store.sortBy).toBe('email')
    expect(store.sortDir).toBe('asc')
  })

  describe('create', () => {
    it('sets creating true then false around a successful call', async () => {
      const created = makeCustomer({ id: '2', fullName: 'New Customer' })
      createCustomerMock.mockResolvedValue(created)

      const store = useCustomersStore()
      const promise = store.create({ fullName: 'New Customer', email: 'new@example.com' })

      expect(store.creating).toBe(true)
      const result = await promise

      expect(store.creating).toBe(false)
      expect(store.createError).toBeNull()
      expect(result).toEqual(created)
    })

    it('maps a 409 response to createError = "duplicateEmail"', async () => {
      createCustomerMock.mockRejectedValue(new ApiError(409, 'A customer with this email already exists.'))

      const store = useCustomersStore()

      await expect(
        store.create({ fullName: 'Dup', email: 'dup@example.com' }),
      ).rejects.toThrow('A customer with this email already exists.')

      expect(store.createError).toBe('duplicateEmail')
      expect(store.creating).toBe(false)
    })

    it('maps any other error to createError = "generic"', async () => {
      createCustomerMock.mockRejectedValue(new Error('network down'))

      const store = useCustomersStore()

      await expect(
        store.create({ fullName: 'Err', email: 'err@example.com' }),
      ).rejects.toThrow('network down')

      expect(store.createError).toBe('generic')
      expect(store.creating).toBe(false)
    })
  })

  describe('getById', () => {
    it('sets current on success', async () => {
      const customer = makeCustomer()
      getCustomerMock.mockResolvedValue(customer)

      const store = useCustomersStore()
      await store.getById('1')

      expect(store.current).toEqual(customer)
      expect(store.notFound).toBe(false)
      expect(store.loadingCurrent).toBe(false)
    })

    it('sets notFound on a 404', async () => {
      getCustomerMock.mockRejectedValue(new ApiError(404, 'Not found'))

      const store = useCustomersStore()
      await store.getById('missing')

      expect(store.current).toBeNull()
      expect(store.notFound).toBe(true)
      expect(store.loadingCurrent).toBe(false)
    })

    it('sets loadError on a non-404 failure', async () => {
      getCustomerMock.mockRejectedValue(new Error('network down'))

      const store = useCustomersStore()
      await store.getById('1')

      expect(store.current).toBeNull()
      expect(store.notFound).toBe(false)
      expect(store.loadError).toBe('errorLoad')
      expect(store.loadingCurrent).toBe(false)
    })
  })

  describe('clearCurrent', () => {
    it('resets current, loadingCurrent, notFound, and loadError', async () => {
      getCustomerMock.mockResolvedValue(makeCustomer())

      const store = useCustomersStore()
      await store.getById('1')
      expect(store.current).not.toBeNull()

      store.clearCurrent()

      expect(store.current).toBeNull()
      expect(store.loadingCurrent).toBe(false)
      expect(store.notFound).toBe(false)
      expect(store.loadError).toBeNull()
    })
  })

  describe('update', () => {
    it('sets current to the updated customer on success', async () => {
      const updated = makeCustomer({ fullName: 'Updated Name' })
      updateCustomerMock.mockResolvedValue(updated)

      const store = useCustomersStore()
      const result = await store.update('1', { fullName: 'Updated Name', email: 'alice@example.com' })

      expect(result).toEqual(updated)
      expect(store.current).toEqual(updated)
      expect(store.updating).toBe(false)
      expect(store.updateError).toBeNull()
    })

    it('maps a 409 response to updateError = "duplicateEmail"', async () => {
      updateCustomerMock.mockRejectedValue(new ApiError(409, 'A customer with this email already exists.'))

      const store = useCustomersStore()

      await expect(
        store.update('1', { fullName: 'Dup', email: 'dup@example.com' }),
      ).rejects.toThrow('A customer with this email already exists.')

      expect(store.updateError).toBe('duplicateEmail')
      expect(store.updating).toBe(false)
    })
  })
})
