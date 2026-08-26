import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCustomerInteractionsStore } from '@/stores/customerInteractions'
import type { getCustomerInteractions } from '@/api/customers'
import type { CustomerInteraction, PagedResult } from '@/types/customers'

const { getCustomerInteractionsMock } = vi.hoisted(() => ({
  getCustomerInteractionsMock: vi.fn<typeof getCustomerInteractions>(),
}))

vi.mock('@/api/customers', () => ({
  getCustomerInteractions: getCustomerInteractionsMock,
}))

function makeInteraction(overrides: Partial<CustomerInteraction> = {}): CustomerInteraction {
  return {
    id: '1',
    type: 'CustomerMessage',
    summary: 'Customer asked a question.',
    occurredAt: '2026-01-01T00:00:00Z',
    actorName: null,
    actorId: null,
    ticketId: null,
    ...overrides,
  }
}

function makePage(
  items: CustomerInteraction[],
  overrides: Partial<PagedResult<CustomerInteraction>> = {},
): PagedResult<CustomerInteraction> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, ...overrides }
}

beforeEach(() => {
  setActivePinia(createPinia())
  getCustomerInteractionsMock.mockReset()
})

describe('customerInteractions store', () => {
  it('has the expected initial state', () => {
    const store = useCustomerInteractionsStore()

    expect(store.items).toEqual([])
    expect(store.page).toBe(1)
    expect(store.pageSize).toBe(20)
    expect(store.totalCount).toBe(0)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() populates items and pagination state on success', async () => {
    const interaction = makeInteraction()
    getCustomerInteractionsMock.mockResolvedValue(makePage([interaction]))

    const store = useCustomerInteractionsStore()
    await store.fetch('customer-1')

    expect(store.items).toEqual([interaction])
    expect(store.totalCount).toBe(1)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
    expect(getCustomerInteractionsMock).toHaveBeenCalledWith('customer-1', 1, 20)
  })

  it('fetch() sets an error and does not throw on failure', async () => {
    getCustomerInteractionsMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerInteractionsStore()
    await expect(store.fetch('customer-1')).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.loading).toBe(false)
    expect(store.items).toEqual([])
  })

  it('retry() re-invokes the API for the last-fetched customer and page', async () => {
    getCustomerInteractionsMock.mockImplementation((_customerId, page) =>
      Promise.resolve(makePage([], { page })),
    )

    const store = useCustomerInteractionsStore()
    await store.fetch('customer-1', 2)
    getCustomerInteractionsMock.mockClear()

    store.retry()
    await vi.waitFor(() => expect(getCustomerInteractionsMock).toHaveBeenCalledTimes(1))

    expect(getCustomerInteractionsMock).toHaveBeenCalledWith('customer-1', 2, 20)
  })

  it('reset() clears all state', async () => {
    getCustomerInteractionsMock.mockResolvedValue(makePage([makeInteraction()], { totalCount: 1 }))

    const store = useCustomerInteractionsStore()
    await store.fetch('customer-1')
    expect(store.items).toHaveLength(1)

    store.reset()

    expect(store.items).toEqual([])
    expect(store.page).toBe(1)
    expect(store.totalCount).toBe(0)
    expect(store.error).toBeNull()

    // retry() after reset is a no-op — no customer id remembered.
    store.retry()
    expect(getCustomerInteractionsMock).toHaveBeenCalledTimes(1)
  })
})
