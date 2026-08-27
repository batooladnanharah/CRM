import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useSlaPoliciesStore } from '@/stores/sla'
import type { createSlaPolicy, deleteSlaPolicy, listSlaPolicies, updateSlaPolicy } from '@/api/sla'
import type { SlaPolicy } from '@/types/tickets'

const { listMock, createMock, updateMock, deleteMock, toastMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listSlaPolicies>(),
  createMock: vi.fn<typeof createSlaPolicy>(),
  updateMock: vi.fn<typeof updateSlaPolicy>(),
  deleteMock: vi.fn<typeof deleteSlaPolicy>(),
  toastMock: {
    success: vi.fn<(input: unknown) => string>(),
    error: vi.fn<(input: unknown) => string>(),
    warning: vi.fn<(input: unknown) => string>(),
    info: vi.fn<(input: unknown) => string>(),
    dismiss: vi.fn<(id: string) => void>(),
    clear: vi.fn<() => void>(),
  },
}))

vi.mock('@/api/sla', () => ({
  listSlaPolicies: listMock,
  createSlaPolicy: createMock,
  updateSlaPolicy: updateMock,
  deleteSlaPolicy: deleteMock,
}))

vi.mock('@/composables/useToast', () => ({ useToast: () => toastMock }))

function makeSlaPolicy(overrides: Partial<SlaPolicy> = {}): SlaPolicy {
  return {
    id: '1',
    name: 'High Priority Policy',
    channel: null,
    priority: 'High',
    firstResponseMinutes: 30,
    resolutionMinutes: 240,
    isDefault: false,
    isActive: true,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listMock.mockReset()
  createMock.mockReset()
  updateMock.mockReset()
  deleteMock.mockReset()
  toastMock.success.mockReset()
  toastMock.error.mockReset()
})

describe('sla policies store', () => {
  it('has the expected initial state', () => {
    const store = useSlaPoliciesStore()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() populates items on success', async () => {
    const policy = makeSlaPolicy()
    listMock.mockResolvedValue([policy])

    const store = useSlaPoliciesStore()
    await store.fetch()

    expect(store.items).toEqual([policy])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() sets errorLoad and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useSlaPoliciesStore()
    await expect(store.fetch()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.items).toEqual([])
  })

  it('create() appends the created policy, sorted by name', async () => {
    listMock.mockResolvedValue([])
    const store = useSlaPoliciesStore()
    await store.fetch()

    createMock.mockResolvedValue(makeSlaPolicy({ id: '2', name: 'Apple Policy' }))
    const result = await store.create({
      name: 'Apple Policy',
      priority: 'High',
      firstResponseMinutes: 30,
      resolutionMinutes: 240,
      isDefault: false,
      isActive: true,
    })

    expect(result.name).toBe('Apple Policy')
    expect(store.items.map((p) => p.name)).toEqual(['Apple Policy'])
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
    expect(toastMock.success).toHaveBeenCalledTimes(1)
  })

  it('create() sets errorSave and rethrows on failure', async () => {
    createMock.mockRejectedValue(new Error('failed'))

    const store = useSlaPoliciesStore()
    await expect(
      store.create({
        name: 'Policy',
        priority: 'High',
        firstResponseMinutes: 30,
        resolutionMinutes: 240,
        isDefault: false,
        isActive: true,
      }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.saving).toBe(false)
  })

  it('update() replaces the policy in place on success', async () => {
    listMock.mockResolvedValue([makeSlaPolicy({ id: '1', name: 'Original' })])
    const store = useSlaPoliciesStore()
    await store.fetch()

    const updated = makeSlaPolicy({ id: '1', name: 'Updated' })
    updateMock.mockResolvedValue(updated)
    await store.update('1', {
      name: 'Updated',
      priority: 'High',
      firstResponseMinutes: 30,
      resolutionMinutes: 240,
      isDefault: false,
      isActive: true,
    })

    expect(store.items[0]).toEqual(updated)
    expect(toastMock.success).toHaveBeenCalledTimes(1)
  })

  it('update() sets errorSave and rethrows on failure', async () => {
    updateMock.mockRejectedValue(new Error('failed'))

    const store = useSlaPoliciesStore()
    await expect(
      store.update('1', {
        name: 'Policy',
        priority: 'High',
        firstResponseMinutes: 30,
        resolutionMinutes: 240,
        isDefault: false,
        isActive: true,
      }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
  })

  it('remove() removes the policy from state on success', async () => {
    listMock.mockResolvedValue([makeSlaPolicy({ id: '1' })])
    const store = useSlaPoliciesStore()
    await store.fetch()

    deleteMock.mockResolvedValue(undefined)
    await store.remove('1')

    expect(store.items).toEqual([])
  })

  it('remove() sets errorDelete and rethrows on failure', async () => {
    deleteMock.mockRejectedValue(new Error('failed'))

    const store = useSlaPoliciesStore()
    await expect(store.remove('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorDelete')
  })
})
