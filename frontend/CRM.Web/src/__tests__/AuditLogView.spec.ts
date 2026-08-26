import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import AuditLogView from '@/modules/security/views/AuditLogView.vue'
import { i18n } from '@/i18n'
import type { AuditLogQuery } from '@/types/security'
import type { AuditLogEntry } from '@/types/security'

function makeEntry(overrides: Partial<AuditLogEntry> = {}): AuditLogEntry {
  return {
    id: 'a1',
    occurredAtUtc: '2026-01-01T00:00:00Z',
    actorUserId: 'admin-1',
    actorEmail: 'admin@crm.local',
    action: 'user.role.assigned',
    targetType: 'user',
    targetId: '1',
    ipAddress: '127.0.0.1',
    payloadJson: '{"before":["agent"],"after":["admin"]}',
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    auditEntries: [] as AuditLogEntry[],
    auditPage: 1,
    auditPageSize: 25,
    auditTotalCount: 0,
    auditLoading: false,
    auditError: null as string | null,
    fetchAuditLog: vi.fn<(filters?: AuditLogQuery) => Promise<void>>(),
    setAuditPage: vi.fn<(page: number, filters?: Partial<AuditLogQuery>) => void>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/security', () => ({
  useSecurityStore: () => fakeStore,
}))

function mountView() {
  return mount(AuditLogView, { global: { plugins: [i18n] } })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('AuditLogView', () => {
  it('calls fetchAuditLog on mount', () => {
    mountView()

    expect(fakeStore.fetchAuditLog).toHaveBeenCalledOnce()
  })

  it('renders a row per audit entry', () => {
    fakeStore = makeFakeStore({ auditEntries: [makeEntry({ action: 'user.login.succeeded' })] })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('user.login.succeeded')
    expect(wrapper.text()).toContain('admin@crm.local')
  })

  it('shows the empty state when there are no entries', () => {
    const wrapper = mountView()

    expect(wrapper.text()).toContain('No audit entries found.')
  })

  it('applies the action filter and refetches from page 1', async () => {
    const wrapper = mountView()

    await wrapper.find('#audit-action').setValue('user.login.failed')
    await wrapper.find('form').trigger('submit')

    expect(fakeStore.fetchAuditLog).toHaveBeenLastCalledWith(
      expect.objectContaining({ action: 'user.login.failed', page: 1 }),
    )
  })

  it('opens the details drawer showing the raw payload JSON', async () => {
    fakeStore = makeFakeStore({ auditEntries: [makeEntry()] })
    const wrapper = mountView()

    await wrapper.find('tbody button').trigger('click')

    expect(wrapper.text()).toContain('{"before":["agent"],"after":["admin"]}')
  })

  it('closes the details drawer when Close is clicked', async () => {
    fakeStore = makeFakeStore({ auditEntries: [makeEntry()] })
    const wrapper = mountView()

    await wrapper.find('tbody button').trigger('click')
    expect(wrapper.find('.drawer').exists()).toBe(true)

    const closeButton = wrapper.findAll('button').find((b) => b.text() === 'Close')!
    await closeButton.trigger('click')

    expect(wrapper.find('.drawer').exists()).toBe(false)
  })

  it('paginates via Next/Prev', async () => {
    fakeStore = makeFakeStore({
      auditEntries: [makeEntry()],
      auditPage: 1,
      auditTotalCount: 50,
      auditPageSize: 25,
    })
    const wrapper = mountView()

    const buttons = wrapper.findAll('button')
    const nextButton = buttons.find((b) => b.text() === 'Next')!
    await nextButton.trigger('click')

    expect(fakeStore.setAuditPage).toHaveBeenCalledWith(2, expect.anything())
  })

  it('shows the error state', () => {
    fakeStore = makeFakeStore({ auditError: 'errorLoad' })
    const wrapper = mountView()

    expect(wrapper.text()).toContain('Could not load the audit log. Please try again.')
  })
})
