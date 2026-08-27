import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useTicketsStore } from '@/stores/tickets'
import { ApiError } from '@/api/http'
import type {
  assignTicket,
  changeTicketPriority,
  changeTicketStatus,
  createTicket,
  escalateTicket,
  fetchEligibleAgents,
  fetchTicketHistory,
  getTicket,
  listTickets,
} from '@/api/tickets'
import type { PagedResult } from '@/types/customers'
import type { EligibleAgent, Ticket, TicketHistoryEntry, TicketListItem } from '@/types/tickets'

const {
  listTicketsMock,
  createTicketMock,
  getTicketMock,
  assignTicketMock,
  changeTicketStatusMock,
  changeTicketPriorityMock,
  fetchTicketHistoryMock,
  fetchEligibleAgentsMock,
  escalateTicketMock,
  toastMock,
} = vi.hoisted(() => ({
  listTicketsMock: vi.fn<typeof listTickets>(),
  createTicketMock: vi.fn<typeof createTicket>(),
  getTicketMock: vi.fn<typeof getTicket>(),
  assignTicketMock: vi.fn<typeof assignTicket>(),
  changeTicketStatusMock: vi.fn<typeof changeTicketStatus>(),
  changeTicketPriorityMock: vi.fn<typeof changeTicketPriority>(),
  fetchTicketHistoryMock: vi.fn<typeof fetchTicketHistory>(),
  fetchEligibleAgentsMock: vi.fn<typeof fetchEligibleAgents>(),
  escalateTicketMock: vi.fn<typeof escalateTicket>(),
  toastMock: {
    success: vi.fn<(input: unknown) => string>(),
    error: vi.fn<(input: unknown) => string>(),
    warning: vi.fn<(input: unknown) => string>(),
    info: vi.fn<(input: unknown) => string>(),
    dismiss: vi.fn<(id: string) => void>(),
    clear: vi.fn<() => void>(),
  },
}))

vi.mock('@/api/tickets', () => ({
  listTickets: listTicketsMock,
  createTicket: createTicketMock,
  getTicket: getTicketMock,
  assignTicket: assignTicketMock,
  changeTicketStatus: changeTicketStatusMock,
  changeTicketPriority: changeTicketPriorityMock,
  fetchTicketHistory: fetchTicketHistoryMock,
  escalateTicket: escalateTicketMock,
  fetchEligibleAgents: fetchEligibleAgentsMock,
}))

vi.mock('@/composables/useToast', () => ({ useToast: () => toastMock }))

function makeTicketListItem(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'Normal',
    assigneeUserId: null,
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

function makeTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    customerEmail: 'alice@example.com',
    title: 'Cannot log in',
    description: 'User reports login failures.',
    status: 'Open',
    priority: 'Normal',
    assigneeUserId: null,
    assigneeDisplayName: null,
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
  return { items, page: 1, pageSize: 20, totalCount: items.length, ...overrides }
}

function makeAgent(overrides: Partial<EligibleAgent> = {}): EligibleAgent {
  return { id: 'agent-1', displayName: 'Second Agent', email: 'second-agent@crm.local', ...overrides }
}

function makeHistoryEntry(overrides: Partial<TicketHistoryEntry> = {}): TicketHistoryEntry {
  return {
    id: 'h1',
    changeType: 'Status',
    oldValue: 'Open',
    newValue: 'InProgress',
    reason: null,
    changedByUserId: 'user-1',
    changedByDisplayName: 'Active Agent',
    changedAtUtc: '2026-01-03T00:00:00Z',
    isSystemActor: false,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listTicketsMock.mockReset()
  createTicketMock.mockReset()
  getTicketMock.mockReset()
  assignTicketMock.mockReset()
  changeTicketStatusMock.mockReset()
  changeTicketPriorityMock.mockReset()
  fetchTicketHistoryMock.mockReset()
  fetchEligibleAgentsMock.mockReset()
  escalateTicketMock.mockReset()
  toastMock.success.mockReset()
  toastMock.error.mockReset()
})

describe('tickets store', () => {
  it('has the expected initial state', () => {
    const store = useTicketsStore()

    expect(store.items).toEqual([])
    expect(store.current).toBeNull()
    expect(store.total).toBe(0)
    expect(store.page).toBe(1)
    expect(store.pageSize).toBe(20)
    expect(store.search).toBe('')
    expect(store.status).toBe('')
    expect(store.priority).toBe('')
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchList() populates items and pagination state on success', async () => {
    const item = makeTicketListItem()
    listTicketsMock.mockResolvedValue(makePage([item]))

    const store = useTicketsStore()
    await store.fetchList()

    expect(store.items).toEqual([item])
    expect(store.total).toBe(1)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchList() sets an error and does not throw when the API call fails', async () => {
    listTicketsMock.mockRejectedValue(new Error('network down'))

    const store = useTicketsStore()
    await expect(store.fetchList()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.loading).toBe(false)
    expect(store.items).toEqual([])
  })

  describe('setSearch', () => {
    beforeEach(() => {
      vi.useFakeTimers()
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('debounces search updates before triggering fetchList', async () => {
      listTicketsMock.mockResolvedValue(makePage([]))

      const store = useTicketsStore()
      store.setSearch('login')

      expect(listTicketsMock).not.toHaveBeenCalled()

      await vi.advanceTimersByTimeAsync(300)

      expect(listTicketsMock).toHaveBeenCalledTimes(1)
      expect(listTicketsMock).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'login', page: 1 }),
      )
    })
  })

  it('setFilters triggers an immediate refetch with the new status/priority', async () => {
    listTicketsMock.mockResolvedValue(makePage([]))

    const store = useTicketsStore()
    store.setFilters({ status: 'Closed', priority: 'High' })

    await vi.waitFor(() => expect(listTicketsMock).toHaveBeenCalledTimes(1))
    expect(store.status).toBe('Closed')
    expect(store.priority).toBe('High')
    expect(listTicketsMock).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'Closed', priority: 'High', page: 1 }),
    )
  })

  it('setPage refetches with the requested page', async () => {
    listTicketsMock.mockResolvedValue(makePage([]))

    const store = useTicketsStore()
    store.setPage(2)

    await vi.waitFor(() => expect(listTicketsMock).toHaveBeenCalledTimes(1))
    expect(listTicketsMock).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }))
  })

  describe('create', () => {
    it('sets creating true then false around a successful call', async () => {
      const created = makeTicket({ id: '2', title: 'New Ticket' })
      createTicketMock.mockResolvedValue(created)

      const store = useTicketsStore()
      const promise = store.create({
        customerId: 'customer-1',
        title: 'New Ticket',
        description: 'Details',
      })

      expect(store.creating).toBe(true)
      const result = await promise

      expect(store.creating).toBe(false)
      expect(store.createError).toBeNull()
      expect(result).toEqual(created)
    })

    it('maps a 400 customer_not_found response to createError = "customerNotFound"', async () => {
      createTicketMock.mockRejectedValue(new ApiError(400, 'customer_not_found'))

      const store = useTicketsStore()

      await expect(
        store.create({ customerId: 'missing', title: 'Orphan', description: 'Details' }),
      ).rejects.toThrow('customer_not_found')

      expect(store.createError).toBe('customerNotFound')
      expect(store.creating).toBe(false)
    })

    it('maps any other error to createError = "generic"', async () => {
      createTicketMock.mockRejectedValue(new Error('network down'))

      const store = useTicketsStore()

      await expect(
        store.create({ customerId: 'customer-1', title: 'Err', description: 'Details' }),
      ).rejects.toThrow('network down')

      expect(store.createError).toBe('generic')
      expect(store.creating).toBe(false)
    })

    it('shows a success toast after a successful create', async () => {
      createTicketMock.mockResolvedValue(makeTicket({ id: '2', title: 'New Ticket' }))

      const store = useTicketsStore()
      await store.create({ customerId: 'customer-1', title: 'New Ticket', description: 'Details' })

      expect(toastMock.success).toHaveBeenCalledTimes(1)
    })

    it('shows an error toast when create fails', async () => {
      createTicketMock.mockRejectedValue(new Error('network down'))

      const store = useTicketsStore()
      await expect(
        store.create({ customerId: 'customer-1', title: 'Err', description: 'Details' }),
      ).rejects.toThrow('network down')

      expect(toastMock.error).toHaveBeenCalledTimes(1)
    })
  })

  describe('fetchOne', () => {
    it('sets current on success', async () => {
      const ticket = makeTicket()
      getTicketMock.mockResolvedValue(ticket)

      const store = useTicketsStore()
      await store.fetchOne('1')

      expect(store.current).toEqual(ticket)
      expect(store.notFound).toBe(false)
      expect(store.loadingCurrent).toBe(false)
    })

    it('sets notFound on a 404', async () => {
      getTicketMock.mockRejectedValue(new ApiError(404, 'Not found'))

      const store = useTicketsStore()
      await store.fetchOne('missing')

      expect(store.current).toBeNull()
      expect(store.notFound).toBe(true)
      expect(store.loadingCurrent).toBe(false)
    })

    it('sets loadError on a non-404 failure', async () => {
      getTicketMock.mockRejectedValue(new Error('network down'))

      const store = useTicketsStore()
      await store.fetchOne('1')

      expect(store.current).toBeNull()
      expect(store.notFound).toBe(false)
      expect(store.loadError).toBe('errorLoad')
      expect(store.loadingCurrent).toBe(false)
    })

    it('round-trips the SLA automation fields unchanged', async () => {
      const ticket = makeTicket({
        sla: {
          policyId: 'policy-1',
          firstResponseDueAtUtc: '2026-01-01T01:00:00Z',
          resolutionDueAtUtc: '2026-01-01T08:00:00Z',
          firstRespondedAtUtc: null,
          resolvedAtUtc: null,
          firstResponseStatus: 'Breached',
          resolutionStatus: 'AtRisk',
          firstResponseBreachedAtUtc: '2026-01-01T01:05:00Z',
          resolutionBreachedAtUtc: null,
          slaLastEvaluatedAtUtc: '2026-01-01T01:05:00Z',
          slaAutoEscalatedAtUtc: '2026-01-01T01:05:00Z',
        },
      })
      getTicketMock.mockResolvedValue(ticket)

      const store = useTicketsStore()
      await store.fetchOne('1')

      expect(store.current?.sla.firstResponseBreachedAtUtc).toBe('2026-01-01T01:05:00Z')
      expect(store.current?.sla.resolutionBreachedAtUtc).toBeNull()
      expect(store.current?.sla.slaLastEvaluatedAtUtc).toBe('2026-01-01T01:05:00Z')
      expect(store.current?.sla.slaAutoEscalatedAtUtc).toBe('2026-01-01T01:05:00Z')
    })
  })

  describe('assign', () => {
    it('sets isAssigning true then false and replaces current on success', async () => {
      const updated = makeTicket({ assigneeUserId: 'agent-1', assigneeDisplayName: 'Second Agent' })
      assignTicketMock.mockResolvedValue(updated)

      const store = useTicketsStore()
      const promise = store.assign('1', 'agent-1')

      expect(store.isAssigning).toBe(true)
      const result = await promise

      expect(store.isAssigning).toBe(false)
      expect(store.actionError).toBeNull()
      expect(result).toEqual(updated)
      expect(store.current).toEqual(updated)
    })

    it('sets actionError to the server message on a 400 and rethrows', async () => {
      assignTicketMock.mockRejectedValue(new ApiError(400, 'invalid_agent'))

      const store = useTicketsStore()

      await expect(store.assign('1', 'not-an-agent')).rejects.toThrow('invalid_agent')

      expect(store.actionError).toBe('invalid_agent')
      expect(store.isAssigning).toBe(false)
      expect(store.current).toBeNull()
      expect(toastMock.error).toHaveBeenCalledTimes(1)
    })

    it('shows a success toast on successful assignment', async () => {
      assignTicketMock.mockResolvedValue(makeTicket({ assigneeUserId: 'agent-1' }))

      const store = useTicketsStore()
      await store.assign('1', 'agent-1')

      expect(toastMock.success).toHaveBeenCalledTimes(1)
    })
  })

  describe('changeStatus', () => {
    it('sets isChangingStatus true then false and replaces current on success', async () => {
      const updated = makeTicket({ status: 'InProgress' })
      changeTicketStatusMock.mockResolvedValue(updated)

      const store = useTicketsStore()
      const promise = store.changeStatus('1', 'InProgress')

      expect(store.isChangingStatus).toBe(true)
      const result = await promise

      expect(store.isChangingStatus).toBe(false)
      expect(store.actionError).toBeNull()
      expect(result).toEqual(updated)
      expect(store.current).toEqual(updated)
      expect(toastMock.success).toHaveBeenCalledTimes(1)
    })

    it('sets actionError on an illegal-transition 400 and does not replace current', async () => {
      changeTicketStatusMock.mockRejectedValue(
        new ApiError(400, 'Cannot transition ticket status from Closed to Open.'),
      )

      const store = useTicketsStore()

      await expect(store.changeStatus('1', 'Open')).rejects.toThrow(
        'Cannot transition ticket status from Closed to Open.',
      )

      expect(store.actionError).toBe('Cannot transition ticket status from Closed to Open.')
      expect(store.isChangingStatus).toBe(false)
      expect(store.current).toBeNull()
    })
  })

  describe('changePriority', () => {
    it('sets isChangingPriority true then false and replaces current on success', async () => {
      const updated = makeTicket({ priority: 'Urgent' })
      changeTicketPriorityMock.mockResolvedValue(updated)

      const store = useTicketsStore()
      const promise = store.changePriority('1', 'Urgent')

      expect(store.isChangingPriority).toBe(true)
      const result = await promise

      expect(store.isChangingPriority).toBe(false)
      expect(store.actionError).toBeNull()
      expect(result).toEqual(updated)
      expect(store.current).toEqual(updated)
    })

    it('sets actionError on failure', async () => {
      changeTicketPriorityMock.mockRejectedValue(new Error('network down'))

      const store = useTicketsStore()

      await expect(store.changePriority('1', 'Urgent')).rejects.toThrow('network down')

      expect(store.actionError).toBe('errorAction')
      expect(store.isChangingPriority).toBe(false)
    })
  })

  describe('loadHistory', () => {
    it('populates history on success', async () => {
      const entry = makeHistoryEntry()
      fetchTicketHistoryMock.mockResolvedValue([entry])

      const store = useTicketsStore()
      await store.loadHistory('1')

      expect(store.history).toEqual([entry])
      expect(store.isLoadingHistory).toBe(false)
      expect(store.actionError).toBeNull()
    })

    it('sets actionError and does not throw on failure', async () => {
      fetchTicketHistoryMock.mockRejectedValue(new Error('network down'))

      const store = useTicketsStore()
      await expect(store.loadHistory('1')).resolves.toBeUndefined()

      expect(store.actionError).toBe('errorLoadHistory')
      expect(store.history).toEqual([])
    })
  })

  describe('loadEligibleAgents', () => {
    it('populates eligibleAgents on success', async () => {
      const agent = makeAgent()
      fetchEligibleAgentsMock.mockResolvedValue([agent])

      const store = useTicketsStore()
      await store.loadEligibleAgents()

      expect(store.eligibleAgents).toEqual([agent])
      expect(store.isLoadingAgents).toBe(false)
      expect(store.actionError).toBeNull()
    })

    it('sets actionError and does not throw on failure', async () => {
      fetchEligibleAgentsMock.mockRejectedValue(new Error('network down'))

      const store = useTicketsStore()
      await expect(store.loadEligibleAgents()).resolves.toBeUndefined()

      expect(store.actionError).toBe('errorLoadAgents')
      expect(store.eligibleAgents).toEqual([])
    })
  })

  describe('escalate', () => {
    it('sets escalating true then false, replaces current, and refetches history on success', async () => {
      const updated = makeTicket({ priority: 'High' })
      escalateTicketMock.mockResolvedValue(updated)
      fetchTicketHistoryMock.mockResolvedValue([makeHistoryEntry({ changeType: 'Escalated' })])

      const store = useTicketsStore()
      const promise = store.escalate('1', 'Customer is a VIP account.')

      expect(store.escalating).toBe(true)
      const result = await promise

      expect(store.escalating).toBe(false)
      expect(store.escalateError).toBeNull()
      expect(result).toEqual(updated)
      expect(store.current).toEqual(updated)
      expect(fetchTicketHistoryMock).toHaveBeenCalledWith('1')
      expect(store.history).toEqual([makeHistoryEntry({ changeType: 'Escalated' })])
    })

    it('sets escalateError to the server message on a 400 and rethrows', async () => {
      escalateTicketMock.mockRejectedValue(new ApiError(400, 'Ticket is already at the highest priority.'))

      const store = useTicketsStore()

      await expect(store.escalate('1', 'Reason')).rejects.toThrow(
        'Ticket is already at the highest priority.',
      )

      expect(store.escalateError).toBe('Ticket is already at the highest priority.')
      expect(store.escalating).toBe(false)
      expect(store.current).toBeNull()
    })
  })
})
