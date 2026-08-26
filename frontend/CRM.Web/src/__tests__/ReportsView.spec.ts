import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import ReportsView from '@/modules/reports/views/ReportsView.vue'
import { i18n } from '@/i18n'
import type { ReportsSummary } from '@/types/reports'

function makeSummary(overrides: Partial<ReportsSummary> = {}): ReportsSummary {
  return {
    ticketVolume: { total: 10, open: 4, resolved: 6 },
    statusDistribution: [
      { status: 'Resolved', count: 3 },
      { status: 'Open', count: 2 },
      { status: 'Closed', count: 3 },
      { status: 'InProgress', count: 2 },
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

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    summary: null as ReportsSummary | null,
    loading: false,
    error: null as string | null,
    lastLoadedAt: null as Date | null,
    fetchSummary: vi.fn<() => Promise<void>>(),
    reset: vi.fn<() => void>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/reports', () => ({
  useReportsStore: () => fakeStore,
}))

function mountView() {
  return mount(ReportsView, { global: { plugins: [i18n] } })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('ReportsView', () => {
  it('calls fetchSummary on mount', () => {
    mountView()

    expect(fakeStore.fetchSummary).toHaveBeenCalledOnce()
  })

  it('calls fetchSummary again when Refresh is clicked', async () => {
    fakeStore = makeFakeStore({ summary: makeSummary() })
    const wrapper = mountView()

    const refreshButton = wrapper.findAll('button').find((b) => b.text() === 'Refresh')!
    await refreshButton.trigger('click')

    expect(fakeStore.fetchSummary).toHaveBeenCalledTimes(2)
  })

  it('shows the error banner when store.error is set', () => {
    fakeStore = makeFakeStore({ error: 'loadFailed' })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('Could not load the report. Please try again.')
  })

  it('renders ticket volume KPIs', () => {
    fakeStore = makeFakeStore({ summary: makeSummary() })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('10')
    expect(wrapper.text()).toContain('4')
    expect(wrapper.text()).toContain('6')
  })

  it('renders the status distribution table in canonical status order', () => {
    fakeStore = makeFakeStore({ summary: makeSummary() })
    const wrapper = mountView()

    const rows = wrapper.findAll('table')[0]!.findAll('tbody tr')
    expect(rows.map((r) => r.text())).toEqual([
      'Open2',
      'In Progress2',
      'Resolved3',
      'Closed3',
    ])
  })

  it('renders the agent performance table', () => {
    fakeStore = makeFakeStore({ summary: makeSummary() })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('Active Agent')
    expect(wrapper.text()).toContain('5')
  })

  it('shows the empty state when no agent has tickets assigned', () => {
    fakeStore = makeFakeStore({ summary: makeSummary({ agentPerformance: [] }) })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('No agent has any tickets assigned yet.')
  })

  it('renders SLA performance rows with counts and percentages', () => {
    fakeStore = makeFakeStore({ summary: makeSummary() })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('Within SLA: 8')
    expect(wrapper.text()).toContain('(80%)')
    expect(wrapper.text()).toContain('At Risk: 1')
    expect(wrapper.text()).toContain('(10%)')
    expect(wrapper.text()).toContain('Breached: 1')
  })

  it('formats a non-null average resolution time as Xh Ym', () => {
    fakeStore = makeFakeStore({
      summary: makeSummary({ resolution: { resolvedTicketCount: 6, averageResolutionMinutes: 90 } }),
    })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('1h 30m')
  })

  it('formats a multi-day average resolution time as Xd Xh Xm', () => {
    fakeStore = makeFakeStore({
      summary: makeSummary({
        resolution: { resolvedTicketCount: 3, averageResolutionMinutes: 25 * 60 + 15 },
      }),
    })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('1d 1h 15m')
  })

  it('shows the none placeholder when averageResolutionMinutes is null', () => {
    fakeStore = makeFakeStore({
      summary: makeSummary({ resolution: { resolvedTicketCount: 0, averageResolutionMinutes: null } }),
    })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('No resolved tickets yet')
  })
})
