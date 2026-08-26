import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import type { listTickets } from '@/api/tickets'
import type { PagedResult } from '@/types/customers'
import type { TicketListItem } from '@/types/tickets'

const { listTicketsMock } = vi.hoisted(() => ({
  listTicketsMock: vi.fn<typeof listTickets>(),
}))

vi.mock('@/api/tickets', () => ({
  listTickets: listTicketsMock,
}))

function makeTicket(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'Normal',
    assigneeUserId: 'agent-1',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    sla: {
      policyId: null,
      firstResponseDueAtUtc: null,
      resolutionDueAtUtc: null,
      firstRespondedAtUtc: null,
      resolvedAtUtc: null,
      firstResponseStatus: 'NotApplicable',
      resolutionStatus: 'NotApplicable',
      firstResponseBreachedAtUtc: null,
      resolutionBreachedAtUtc: null,
      slaLastEvaluatedAtUtc: null,
      slaAutoEscalatedAtUtc: null,
    },
    ...overrides,
  }
}

function makePage(
  items: TicketListItem[],
  overrides: Partial<PagedResult<TicketListItem>> = {},
): PagedResult<TicketListItem> {
  return { items, page: 1, pageSize: 25, totalCount: items.length, ...overrides }
}

function loginAsAgent() {
  const authStore = useAuthStore()
  authStore.token = 'a-valid-token'
  authStore.user = { id: 'agent-1', name: 'Active Agent', email: 'agent@crm.local', roles: ['agent'] }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listTicketsMock.mockReset()
})

describe('dashboard store', () => {
  it('has the expected initial state', () => {
    const store = useDashboardStore()

    expect(store.summary).toBeNull()
    expect(store.myOpenTickets).toEqual([])
    expect(store.myTasks).toEqual([])
    expect(store.recentCustomers).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('loadAll() sets loading true then false, and populates all slices', async () => {
    loginAsAgent()
    listTicketsMock
      .mockResolvedValueOnce(makePage([makeTicket()]))
      .mockResolvedValueOnce(makePage([], { totalCount: 3 }))

    const store = useDashboardStore()
    const promise = store.loadAll()

    expect(store.loading).toBe(true)
    await promise

    expect(store.loading).toBe(false)
    expect(store.myOpenTickets).toHaveLength(1)
    expect(store.summary).toEqual({
      openAssignedCount: 1,
      needsActionCount: 1,
      resolvedLast7DaysCount: 3,
      slaAtRiskCount: 0,
    })
  })

  it('passes the signed-in agent id as assigneeId on both calls', async () => {
    loginAsAgent()
    listTicketsMock.mockResolvedValue(makePage([]))

    const store = useDashboardStore()
    await store.loadAll()

    expect(listTicketsMock).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ assigneeId: 'agent-1' }),
    )
    expect(listTicketsMock).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ assigneeId: 'agent-1', status: 'Resolved' }),
    )
  })

  it('sets error when there is no signed-in user', async () => {
    const store = useDashboardStore()
    await store.loadAll()

    expect(store.error).toBe('noUser')
    expect(listTicketsMock).not.toHaveBeenCalled()
  })

  it('on one rejected call, the other slice still populates and error is set', async () => {
    loginAsAgent()
    listTicketsMock
      .mockResolvedValueOnce(makePage([makeTicket()]))
      .mockRejectedValueOnce(new Error('network down'))

    const store = useDashboardStore()
    await store.loadAll()

    expect(store.error).toBe('loadFailed')
    expect(store.myOpenTickets).toHaveLength(1)
  })

  it('myTasks contains only Open tickets sorted by priority desc then oldest first', async () => {
    loginAsAgent()
    listTicketsMock
      .mockResolvedValueOnce(
        makePage([
          makeTicket({ id: '1', status: 'Open', priority: 'Low', createdAtUtc: '2026-01-02T00:00:00Z' }),
          makeTicket({ id: '2', status: 'Open', priority: 'Urgent', createdAtUtc: '2026-01-03T00:00:00Z' }),
          makeTicket({ id: '3', status: 'InProgress', priority: 'Urgent', createdAtUtc: '2026-01-01T00:00:00Z' }),
          makeTicket({ id: '4', status: 'Open', priority: 'Urgent', createdAtUtc: '2026-01-01T00:00:00Z' }),
        ]),
      )
      .mockResolvedValueOnce(makePage([]))

    const store = useDashboardStore()
    await store.loadAll()

    expect(store.myTasks.map((t) => t.id)).toEqual(['4', '2', '1'])
  })

  it('recentCustomers is deduplicated by customer id and capped at 8', async () => {
    loginAsAgent()
    const assigned = Array.from({ length: 6 }, (_, i) =>
      makeTicket({
        id: `assigned-${i}`,
        customerId: `customer-${i}`,
        customerName: `Customer ${i}`,
        updatedAtUtc: `2026-01-${10 + i}T00:00:00Z`,
      }),
    )
    const resolved = Array.from({ length: 5 }, (_, i) =>
      makeTicket({
        id: `resolved-${i}`,
        customerId: `customer-${i}`,
        customerName: `Customer ${i}`,
        updatedAtUtc: `2026-01-${20 + i}T00:00:00Z`,
      }),
    )

    listTicketsMock
      .mockResolvedValueOnce(makePage(assigned))
      .mockResolvedValueOnce(makePage(resolved, { totalCount: resolved.length }))

    const store = useDashboardStore()
    await store.loadAll()

    expect(store.recentCustomers).toHaveLength(6)
    expect(new Set(store.recentCustomers.map((c) => c.id)).size).toBe(6)
  })

  it('slaAtRiskCount counts open High/Urgent tickets older than 24 hours', async () => {
    loginAsAgent()
    const old = new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString()
    const recent = new Date().toISOString()

    listTicketsMock
      .mockResolvedValueOnce(
        makePage([
          makeTicket({ id: '1', status: 'Open', priority: 'Urgent', createdAtUtc: old }),
          makeTicket({ id: '2', status: 'Open', priority: 'High', createdAtUtc: recent }),
          makeTicket({ id: '3', status: 'Open', priority: 'Low', createdAtUtc: old }),
        ]),
      )
      .mockResolvedValueOnce(makePage([]))

    const store = useDashboardStore()
    await store.loadAll()

    expect(store.summary?.slaAtRiskCount).toBe(1)
  })

  it('refresh() does not toggle loading', async () => {
    loginAsAgent()
    listTicketsMock.mockResolvedValue(makePage([]))

    const store = useDashboardStore()
    await store.loadAll()

    const promise = store.refresh()
    expect(store.loading).toBe(false)
    await promise
  })
})
