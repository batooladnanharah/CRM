import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import MetricsStrip from '@/modules/dashboard/components/MetricsStrip.vue'
import { i18n } from '@/i18n'
import type { DashboardSummary } from '@/types/dashboard'

const mountOptions = {
  global: {
    plugins: [i18n],
  },
}

function makeSummary(overrides: Partial<DashboardSummary> = {}): DashboardSummary {
  return {
    openAssignedCount: 3,
    needsActionCount: 2,
    resolvedLast7DaysCount: 5,
    slaAtRiskCount: 1,
    ...overrides,
  }
}

describe('MetricsStrip', () => {
  it('renders four tiles bound to the summary', () => {
    const wrapper = mount(MetricsStrip, {
      props: { summary: makeSummary(), loading: false },
      ...mountOptions,
    })

    expect(wrapper.text()).toContain('3')
    expect(wrapper.text()).toContain('2')
    expect(wrapper.text()).toContain('5')
    expect(wrapper.text()).toContain('1')
    expect(wrapper.text()).toContain('Open tickets assigned to me')
    expect(wrapper.text()).toContain('Needs action')
    expect(wrapper.text()).toContain('Resolved (7d)')
    expect(wrapper.text()).toContain('SLA at risk')
  })

  it('renders zeros when summary is null', () => {
    const wrapper = mount(MetricsStrip, {
      props: { summary: null, loading: false },
      ...mountOptions,
    })

    const strongs = wrapper.findAll('strong')
    expect(strongs).toHaveLength(4)
    strongs.forEach((el) => expect(el.text()).toBe('0'))
  })

  it('renders skeleton tiles while loading', () => {
    const wrapper = mount(MetricsStrip, {
      props: { summary: null, loading: true },
      ...mountOptions,
    })

    expect(wrapper.findAll('.skeleton')).toHaveLength(4)
    expect(wrapper.findAll('strong')).toHaveLength(0)
  })
})
