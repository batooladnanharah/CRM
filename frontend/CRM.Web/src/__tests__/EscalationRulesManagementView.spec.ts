import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import EscalationRulesManagementView from '@/modules/sla/views/EscalationRulesManagementView.vue'
import { i18n } from '@/i18n'
import type {
  activateEscalationRule,
  createEscalationRule,
  deactivateEscalationRule,
  deleteEscalationRule,
  listEscalationRules,
  updateEscalationRule,
} from '@/api/escalationRules'
import type { EscalationRule } from '@/types/notifications'

const { listMock, createMock, updateMock, activateMock, deactivateMock, deleteMock, confirmMock } = vi.hoisted(
  () => ({
    listMock: vi.fn<typeof listEscalationRules>(),
    createMock: vi.fn<typeof createEscalationRule>(),
    updateMock: vi.fn<typeof updateEscalationRule>(),
    activateMock: vi.fn<typeof activateEscalationRule>(),
    deactivateMock: vi.fn<typeof deactivateEscalationRule>(),
    deleteMock: vi.fn<typeof deleteEscalationRule>(),
    confirmMock: vi.fn<() => Promise<boolean>>(),
  }),
)

vi.mock('@/api/escalationRules', () => ({
  listEscalationRules: listMock,
  createEscalationRule: createMock,
  updateEscalationRule: updateMock,
  activateEscalationRule: activateMock,
  deactivateEscalationRule: deactivateMock,
  deleteEscalationRule: deleteMock,
}))

vi.mock('@/composables/useConfirm', () => ({ confirm: confirmMock }))

function makeRule(overrides: Partial<EscalationRule> = {}): EscalationRule {
  return {
    id: '1',
    name: 'Notify agent on breach',
    trigger: 'Breached',
    notifyAgent: true,
    notifyManager: false,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listMock.mockReset()
  createMock.mockReset()
  updateMock.mockReset()
  activateMock.mockReset()
  deactivateMock.mockReset()
  deleteMock.mockReset()
  confirmMock.mockReset()
})

function mountView() {
  return mount(EscalationRulesManagementView, { global: { plugins: [i18n] } })
}

describe('EscalationRulesManagementView', () => {
  it('renders the list of escalation rules', async () => {
    listMock.mockResolvedValue([makeRule({ name: 'Manager breach alert' })])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Manager breach alert')
  })

  it('renders the empty state when there are no rules', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No escalation rules yet.')
  })

  it('shows a validation error when neither notify action is selected', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#escalation-rule-name').setValue('Bad Rule')
    await wrapper.find('.escalation-rule-form input[type="checkbox"]').setValue(false)
    await wrapper.find('.escalation-rule-form').trigger('submit')
    await flushPromises()

    expect(createMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Select at least one notification action.')
  })

  it('creates a new rule with valid input', async () => {
    listMock.mockResolvedValue([])
    createMock.mockResolvedValue(makeRule({ id: 'new-1', name: 'New Rule' }))

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#escalation-rule-name').setValue('New Rule')
    await wrapper.find('.escalation-rule-form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith({
      name: 'New Rule',
      trigger: 'AtRisk',
      notifyAgent: true,
      notifyManager: false,
      isActive: true,
    })
    expect(wrapper.text()).toContain('New Rule')
  })

  it('activates and deactivates a rule via row action', async () => {
    listMock.mockResolvedValue([makeRule({ id: '1', isActive: false })])
    activateMock.mockResolvedValue(makeRule({ id: '1', isActive: true }))

    const wrapper = mountView()
    await flushPromises()

    const activateButton = wrapper.findAll('button').find((b) => b.text() === 'Activate')!
    await activateButton.trigger('click')
    await flushPromises()

    expect(activateMock).toHaveBeenCalledWith('1')
  })

  it('prefills the edit form and submits the update', async () => {
    listMock.mockResolvedValue([makeRule({ id: '1', name: 'Original Rule' })])
    updateMock.mockResolvedValue(makeRule({ id: '1', name: 'Updated Rule' }))

    const wrapper = mountView()
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')

    const nameInput = wrapper.find('.escalation-rule-inline-form input[type="text"]')
    expect((nameInput.element as HTMLInputElement).value).toBe('Original Rule')

    await nameInput.setValue('Updated Rule')
    await wrapper.find('.escalation-rule-inline-form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Updated Rule')
  })

  it('deletes a rule after confirmation', async () => {
    listMock.mockResolvedValue([makeRule({ id: '1' })])
    deleteMock.mockResolvedValue(undefined)
    confirmMock.mockResolvedValue(true)

    const wrapper = mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith('1')
  })
})
