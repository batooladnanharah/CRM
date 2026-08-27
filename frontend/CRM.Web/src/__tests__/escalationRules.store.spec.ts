import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useEscalationRulesStore } from '@/stores/escalationRules'
import type {
  activateEscalationRule,
  createEscalationRule,
  deactivateEscalationRule,
  deleteEscalationRule,
  listEscalationRules,
  updateEscalationRule,
} from '@/api/escalationRules'
import type { EscalationRule } from '@/types/notifications'

const { listMock, createMock, updateMock, activateMock, deactivateMock, deleteMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listEscalationRules>(),
  createMock: vi.fn<typeof createEscalationRule>(),
  updateMock: vi.fn<typeof updateEscalationRule>(),
  activateMock: vi.fn<typeof activateEscalationRule>(),
  deactivateMock: vi.fn<typeof deactivateEscalationRule>(),
  deleteMock: vi.fn<typeof deleteEscalationRule>(),
}))

vi.mock('@/api/escalationRules', () => ({
  listEscalationRules: listMock,
  createEscalationRule: createMock,
  updateEscalationRule: updateMock,
  activateEscalationRule: activateMock,
  deactivateEscalationRule: deactivateMock,
  deleteEscalationRule: deleteMock,
}))

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
})

describe('escalation rules store', () => {
  it('has the expected initial state', () => {
    const store = useEscalationRulesStore()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() populates items on success', async () => {
    const rule = makeRule()
    listMock.mockResolvedValue([rule])

    const store = useEscalationRulesStore()
    await store.fetch()

    expect(store.items).toEqual([rule])
    expect(store.error).toBeNull()
  })

  it('fetch() sets an error and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useEscalationRulesStore()
    await expect(store.fetch()).resolves.toBeUndefined()

    expect(store.error).toBeTruthy()
    expect(store.items).toEqual([])
  })

  it('create() prepends the created rule', async () => {
    const created = makeRule({ id: '2', name: 'New Rule' })
    createMock.mockResolvedValue(created)

    const store = useEscalationRulesStore()
    const result = await store.create({
      name: 'New Rule',
      trigger: 'AtRisk',
      notifyAgent: true,
      notifyManager: false,
      isActive: true,
    })

    expect(result).toEqual(created)
    expect(store.items[0]).toEqual(created)
    expect(store.saving).toBe(false)
  })

  it('create() sets an error and rethrows on failure', async () => {
    createMock.mockRejectedValue(new Error('failed'))

    const store = useEscalationRulesStore()
    await expect(
      store.create({ name: 'Rule', trigger: 'AtRisk', notifyAgent: true, notifyManager: false, isActive: true }),
    ).rejects.toThrow('failed')

    expect(store.error).toBeTruthy()
    expect(store.saving).toBe(false)
  })

  it('update() replaces the rule in place', async () => {
    listMock.mockResolvedValue([makeRule({ id: '1', name: 'Original' })])
    const store = useEscalationRulesStore()
    await store.fetch()

    const updated = makeRule({ id: '1', name: 'Updated' })
    updateMock.mockResolvedValue(updated)
    await store.update('1', {
      name: 'Updated',
      trigger: 'Breached',
      notifyAgent: true,
      notifyManager: false,
      isActive: true,
    })

    expect(store.items[0]).toEqual(updated)
  })

  it('activate()/deactivate() toggle the rule in place', async () => {
    listMock.mockResolvedValue([makeRule({ id: '1', isActive: false })])
    const store = useEscalationRulesStore()
    await store.fetch()

    activateMock.mockResolvedValue(makeRule({ id: '1', isActive: true }))
    await store.activate('1')
    expect(store.items[0]?.isActive).toBe(true)

    deactivateMock.mockResolvedValue(makeRule({ id: '1', isActive: false }))
    await store.deactivate('1')
    expect(store.items[0]?.isActive).toBe(false)
  })

  it('remove() removes the rule from state on success', async () => {
    listMock.mockResolvedValue([makeRule({ id: '1' })])
    const store = useEscalationRulesStore()
    await store.fetch()

    deleteMock.mockResolvedValue(undefined)
    await store.remove('1')

    expect(store.items).toEqual([])
  })

  it('remove() sets an error and rethrows on failure', async () => {
    deleteMock.mockRejectedValue(new Error('failed'))

    const store = useEscalationRulesStore()
    await expect(store.remove('1')).rejects.toThrow('failed')

    expect(store.error).toBeTruthy()
  })
})
