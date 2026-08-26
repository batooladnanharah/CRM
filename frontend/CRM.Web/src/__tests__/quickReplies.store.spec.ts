import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useQuickRepliesStore } from '@/stores/quickReplies'
import type {
  createQuickReply,
  deleteQuickReply,
  listQuickReplies,
  updateQuickReply,
} from '@/api/quickReplies'
import type { QuickReply } from '@/types/tickets'

const { listMock, createMock, updateMock, deleteMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listQuickReplies>(),
  createMock: vi.fn<typeof createQuickReply>(),
  updateMock: vi.fn<typeof updateQuickReply>(),
  deleteMock: vi.fn<typeof deleteQuickReply>(),
}))

vi.mock('@/api/quickReplies', () => ({
  listQuickReplies: listMock,
  createQuickReply: createMock,
  updateQuickReply: updateMock,
  deleteQuickReply: deleteMock,
}))

function makeQuickReply(overrides: Partial<QuickReply> = {}): QuickReply {
  return {
    id: '1',
    title: 'Greeting',
    content: 'Hello and welcome!',
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
})

describe('quickReplies store', () => {
  it('has the expected initial state', () => {
    const store = useQuickRepliesStore()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
    expect(store.search).toBe('')
  })

  it('fetch() populates items on success', async () => {
    const reply = makeQuickReply()
    listMock.mockResolvedValue([reply])

    const store = useQuickRepliesStore()
    await store.fetch()

    expect(store.items).toEqual([reply])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetch() sets errorLoad and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useQuickRepliesStore()
    await expect(store.fetch()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.items).toEqual([])
  })

  it('fetch() passes the search term through to the API', async () => {
    listMock.mockResolvedValue([])

    const store = useQuickRepliesStore()
    await store.fetch('password')

    expect(store.search).toBe('password')
    expect(listMock).toHaveBeenCalledWith('password')
  })

  it('create() appends the created reply, sorted by title', async () => {
    listMock.mockResolvedValue([])
    const store = useQuickRepliesStore()
    await store.fetch()

    createMock.mockResolvedValue(makeQuickReply({ id: '2', title: 'Apple' }))
    const result = await store.create({ title: 'Apple', content: 'Content' })

    expect(result.title).toBe('Apple')
    expect(store.items.map((q) => q.title)).toEqual(['Apple'])
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('create() sets errorSave and rethrows on failure', async () => {
    createMock.mockRejectedValue(new Error('failed'))

    const store = useQuickRepliesStore()
    await expect(store.create({ title: 'Title', content: 'Content' })).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.saving).toBe(false)
  })

  it('update() replaces the reply in place on success', async () => {
    listMock.mockResolvedValue([makeQuickReply({ id: '1', title: 'Original' })])
    const store = useQuickRepliesStore()
    await store.fetch()

    const updated = makeQuickReply({ id: '1', title: 'Updated' })
    updateMock.mockResolvedValue(updated)
    await store.update('1', { title: 'Updated', content: 'Content', isActive: true })

    expect(store.items[0]).toEqual(updated)
  })

  it('update() sets errorSave and rethrows on failure', async () => {
    updateMock.mockRejectedValue(new Error('failed'))

    const store = useQuickRepliesStore()
    await expect(
      store.update('1', { title: 'Title', content: 'Content', isActive: true }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
  })

  it('remove() removes the reply from state on success', async () => {
    listMock.mockResolvedValue([makeQuickReply({ id: '1' })])
    const store = useQuickRepliesStore()
    await store.fetch()

    deleteMock.mockResolvedValue(undefined)
    await store.remove('1')

    expect(store.items).toEqual([])
  })

  it('remove() sets errorDelete and rethrows on failure', async () => {
    deleteMock.mockRejectedValue(new Error('failed'))

    const store = useQuickRepliesStore()
    await expect(store.remove('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorDelete')
  })
})
