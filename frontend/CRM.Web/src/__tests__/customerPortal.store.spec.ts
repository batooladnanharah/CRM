import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import type {
  createPortalTicket,
  fetchPortalDashboard,
  fetchPortalTicket,
  fetchPortalTickets,
} from '@/api/customerPortal'
import type { CustomerDashboard, CustomerTicketDetails, CustomerTicketListItem } from '@/types/customerPortal'

const { fetchPortalDashboardMock, fetchPortalTicketsMock, fetchPortalTicketMock, createPortalTicketMock } =
  vi.hoisted(() => ({
    fetchPortalDashboardMock: vi.fn<typeof fetchPortalDashboard>(),
    fetchPortalTicketsMock: vi.fn<typeof fetchPortalTickets>(),
    fetchPortalTicketMock: vi.fn<typeof fetchPortalTicket>(),
    createPortalTicketMock: vi.fn<typeof createPortalTicket>(),
  }))

vi.mock('@/api/customerPortal', () => ({
  fetchPortalDashboard: fetchPortalDashboardMock,
  fetchPortalTickets: fetchPortalTicketsMock,
  fetchPortalTicket: fetchPortalTicketMock,
  createPortalTicket: createPortalTicketMock,
}))

function makeTicketListItem(overrides: Partial<CustomerTicketListItem> = {}): CustomerTicketListItem {
  return {
    id: '1',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'Normal',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeDashboard(overrides: Partial<CustomerDashboard> = {}): CustomerDashboard {
  return {
    openCount: 1,
    pendingCount: 2,
    resolvedCount: 3,
    recentTickets: [makeTicketListItem()],
    ...overrides,
  }
}

function makeTicketDetails(overrides: Partial<CustomerTicketDetails> = {}): CustomerTicketDetails {
  return {
    id: '1',
    title: 'Cannot log in',
    description: 'Details here.',
    status: 'Open',
    priority: 'Normal',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    messages: [],
    history: [],
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  fetchPortalDashboardMock.mockReset()
  fetchPortalTicketsMock.mockReset()
  fetchPortalTicketMock.mockReset()
  createPortalTicketMock.mockReset()
})

describe('customerPortal store', () => {
  it('has the expected initial state', () => {
    const store = useCustomerPortalStore()

    expect(store.dashboard).toBeNull()
    expect(store.tickets).toEqual([])
    expect(store.currentTicket).toBeNull()
    expect(store.loading).toBe(false)
    expect(store.creating).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchDashboard() populates dashboard on success', async () => {
    const dashboard = makeDashboard()
    fetchPortalDashboardMock.mockResolvedValue(dashboard)

    const store = useCustomerPortalStore()
    await store.fetchDashboard()

    expect(store.dashboard).toEqual(dashboard)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchDashboard() sets errorLoad and does not throw on failure', async () => {
    fetchPortalDashboardMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchDashboard()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.dashboard).toBeNull()
  })

  it('fetchTickets() populates tickets on success', async () => {
    const item = makeTicketListItem()
    fetchPortalTicketsMock.mockResolvedValue([item])

    const store = useCustomerPortalStore()
    await store.fetchTickets()

    expect(store.tickets).toEqual([item])
    expect(store.error).toBeNull()
  })

  it('fetchTickets() sets errorLoad and does not throw on failure', async () => {
    fetchPortalTicketsMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchTickets()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.tickets).toEqual([])
  })

  it('fetchTicket() populates currentTicket on success', async () => {
    const details = makeTicketDetails()
    fetchPortalTicketMock.mockResolvedValue(details)

    const store = useCustomerPortalStore()
    await store.fetchTicket('1')

    expect(store.currentTicket).toEqual(details)
    expect(fetchPortalTicketMock).toHaveBeenCalledWith('1')
  })

  it('fetchTicket() sets errorLoad and clears currentTicket on failure', async () => {
    fetchPortalTicketMock.mockRejectedValue(new Error('not found'))

    const store = useCustomerPortalStore()
    await expect(store.fetchTicket('missing')).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.currentTicket).toBeNull()
  })

  it('createTicket() returns the created ticket on success', async () => {
    const details = makeTicketDetails({ id: 'new-1', title: 'New Ticket' })
    createPortalTicketMock.mockResolvedValue(details)

    const store = useCustomerPortalStore()
    const result = await store.createTicket({
      title: 'New Ticket', description: 'Something broke.', priority: 'High',
    })

    expect(result).toEqual(details)
    expect(store.creating).toBe(false)
    expect(store.error).toBeNull()
  })

  it('createTicket() sets errorSave and rethrows on failure', async () => {
    createPortalTicketMock.mockRejectedValue(new Error('failed'))

    const store = useCustomerPortalStore()
    await expect(
      store.createTicket({ title: 'Title', description: 'Description' }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.creating).toBe(false)
  })
})
