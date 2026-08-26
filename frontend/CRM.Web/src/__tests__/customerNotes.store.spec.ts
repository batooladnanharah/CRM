import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCustomerNotesStore } from '@/stores/customerNotes'
import { ApiError } from '@/api/http'
import type {
  createCustomerNote,
  deleteCustomerNote,
  listCustomerNotes,
  updateCustomerNote,
} from '@/api/customers'
import type { CustomerNote } from '@/types/customers'

const { listMock, createMock, updateMock, deleteMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listCustomerNotes>(),
  createMock: vi.fn<typeof createCustomerNote>(),
  updateMock: vi.fn<typeof updateCustomerNote>(),
  deleteMock: vi.fn<typeof deleteCustomerNote>(),
}))

vi.mock('@/api/customers', () => ({
  listCustomerNotes: listMock,
  createCustomerNote: createMock,
  updateCustomerNote: updateMock,
  deleteCustomerNote: deleteMock,
}))

function makeNote(overrides: Partial<CustomerNote> = {}): CustomerNote {
  return {
    id: '1',
    customerId: 'customer-1',
    authorId: 'author-1',
    authorDisplayName: 'Active Agent',
    content: 'Original note',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
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

describe('customerNotes store', () => {
  it('has the expected initial state', () => {
    const store = useCustomerNotesStore()

    expect(store.notes).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchNotes() populates notes newest-first on success', async () => {
    const older = makeNote({ id: '1', createdAtUtc: '2026-01-01T00:00:00Z' })
    const newer = makeNote({ id: '2', createdAtUtc: '2026-01-02T00:00:00Z' })
    listMock.mockResolvedValue([older, newer])

    const store = useCustomerNotesStore()
    await store.fetchNotes('customer-1')

    expect(store.notes.map((n) => n.id)).toEqual(['2', '1'])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchNotes() sets errorLoad and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerNotesStore()
    await expect(store.fetchNotes('customer-1')).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.notes).toEqual([])
  })

  it('addNote() prepends the created note and clears the form state', async () => {
    const created = makeNote({ id: 'new-1' })
    createMock.mockResolvedValue(created)

    const store = useCustomerNotesStore()
    const result = await store.addNote('customer-1', 'New note')

    expect(result).toEqual(created)
    expect(store.notes).toEqual([created])
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('addNote() sets errorSave and rethrows on failure', async () => {
    createMock.mockRejectedValue(new Error('failed'))

    const store = useCustomerNotesStore()
    await expect(store.addNote('customer-1', 'New note')).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.saving).toBe(false)
  })

  it('editNote() replaces the note in place on success', async () => {
    const existing = makeNote({ id: '1', content: 'Original' })
    listMock.mockResolvedValue([existing])
    const updated = makeNote({ id: '1', content: 'Updated', updatedAtUtc: '2026-01-03T00:00:00Z' })
    updateMock.mockResolvedValue(updated)

    const store = useCustomerNotesStore()
    await store.fetchNotes('customer-1')
    await store.editNote('customer-1', '1', 'Updated')

    expect(store.notes[0]).toEqual(updated)
    expect(store.error).toBeNull()
  })

  it('editNote() maps a 403 response to errorForbidden', async () => {
    updateMock.mockRejectedValue(new ApiError(403, 'Forbidden'))

    const store = useCustomerNotesStore()
    await expect(store.editNote('customer-1', '1', 'Updated')).rejects.toBeInstanceOf(ApiError)

    expect(store.error).toBe('errorForbidden')
  })

  it('editNote() maps a non-403 failure to errorSave', async () => {
    updateMock.mockRejectedValue(new Error('failed'))

    const store = useCustomerNotesStore()
    await expect(store.editNote('customer-1', '1', 'Updated')).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
  })

  it('removeNote() removes the note from state on success', async () => {
    const existing = makeNote({ id: '1' })
    listMock.mockResolvedValue([existing])
    deleteMock.mockResolvedValue(undefined)

    const store = useCustomerNotesStore()
    await store.fetchNotes('customer-1')
    await store.removeNote('customer-1', '1')

    expect(store.notes).toEqual([])
    expect(store.error).toBeNull()
  })

  it('removeNote() maps a 403 response to errorForbidden', async () => {
    deleteMock.mockRejectedValue(new ApiError(403, 'Forbidden'))

    const store = useCustomerNotesStore()
    await expect(store.removeNote('customer-1', '1')).rejects.toBeInstanceOf(ApiError)

    expect(store.error).toBe('errorForbidden')
  })

  it('removeNote() maps a non-403 failure to errorDelete', async () => {
    deleteMock.mockRejectedValue(new Error('failed'))

    const store = useCustomerNotesStore()
    await expect(store.removeNote('customer-1', '1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorDelete')
  })
})
