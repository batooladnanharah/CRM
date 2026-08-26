import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useReportsStore } from '@/stores/reports'
import type { getReportsSummary } from '@/api/reports'
import type { ReportsSummary } from '@/types/reports'

const { getReportsSummaryMock } = vi.hoisted(() => ({
  getReportsSummaryMock: vi.fn<typeof getReportsSummary>(),
}))

vi.mock('@/api/reports', () => ({
  getReportsSummary: getReportsSummaryMock,
}))

function makeSummary(overrides: Partial<ReportsSummary> = {}): ReportsSummary {
  return {
    ticketVolume: { total: 10, open: 4, resolved: 6 },
    statusDistribution: [
      { status: 'Open', count: 2 },
      { status: 'InProgress', count: 2 },
      { status: 'Resolved', count: 3 },
      { status: 'Closed', count: 3 },
    ],
    agentPerformance: [{ agentId: 'agent-1', displayName: 'Active Agent', ticketCount: 5 }],
    slaPerformance: {
      totalEvaluated: 10, withinSla: 8, atRisk: 1, breached: 1,
      withinSlaPercent: 80, atRiskPercent: 10, breachedPercent: 10,
    },
    resolution: { resolvedTicketCount: 6, averageResolutionMinutes: 90 },
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  getReportsSummaryMock.mockReset()
})

describe('reports store', () => {
  it('has the expected initial state', () => {
    const store = useReportsStore()

    expect(store.summary).toBeNull()
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
    expect(store.lastLoadedAt).toBeNull()
  })

  it('fetchSummary() populates summary and lastLoadedAt on success', async () => {
    const summary = makeSummary()
    getReportsSummaryMock.mockResolvedValue(summary)

    const store = useReportsStore()
    await store.fetchSummary()

    expect(store.summary).toEqual(summary)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
    expect(store.lastLoadedAt).toBeInstanceOf(Date)
  })

  it('fetchSummary() sets loadFailed and does not throw on failure', async () => {
    getReportsSummaryMock.mockRejectedValue(new Error('network down'))

    const store = useReportsStore()
    await expect(store.fetchSummary()).resolves.toBeUndefined()

    expect(store.error).toBe('loadFailed')
    expect(store.summary).toBeNull()
    expect(store.loading).toBe(false)
  })

  it('reset() clears all state', async () => {
    getReportsSummaryMock.mockResolvedValue(makeSummary())
    const store = useReportsStore()
    await store.fetchSummary()

    store.reset()

    expect(store.summary).toBeNull()
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
    expect(store.lastLoadedAt).toBeNull()
  })
})
