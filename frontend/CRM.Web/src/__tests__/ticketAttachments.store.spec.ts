import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useTicketAttachmentsStore } from '@/stores/ticketAttachments'
import { ApiError } from '@/api/http'
import type {
  deleteTicketAttachment,
  downloadTicketAttachment,
  listTicketAttachments,
  uploadTicketAttachment,
} from '@/api/ticketAttachments'
import type { TicketAttachment } from '@/types/tickets'

const { listMock, uploadMock, deleteMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listTicketAttachments>(),
  uploadMock: vi.fn<typeof uploadTicketAttachment>(),
  deleteMock: vi.fn<typeof deleteTicketAttachment>(),
}))

vi.mock('@/api/ticketAttachments', () => ({
  listTicketAttachments: listMock,
  uploadTicketAttachment: uploadMock,
  deleteTicketAttachment: deleteMock,
  downloadTicketAttachment: vi.fn<typeof downloadTicketAttachment>(),
}))

function makeAttachment(overrides: Partial<TicketAttachment> = {}): TicketAttachment {
  return {
    id: '1',
    ticketId: 'ticket-1',
    originalFileName: 'notes.txt',
    contentType: 'text/plain',
    fileSize: 1024,
    uploadedByUserId: 'author-1',
    uploadedByDisplayName: 'Active Agent',
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listMock.mockReset()
  uploadMock.mockReset()
  deleteMock.mockReset()
})

describe('ticketAttachments store', () => {
  it('has the expected initial state', () => {
    const store = useTicketAttachmentsStore()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.uploading).toBe(false)
    expect(store.deletingId).toBeNull()
    expect(store.error).toBeNull()
  })

  it('fetchAttachments() populates items on success', async () => {
    const attachment = makeAttachment()
    listMock.mockResolvedValue([attachment])

    const store = useTicketAttachmentsStore()
    await store.fetchAttachments('ticket-1')

    expect(store.items).toEqual([attachment])
    expect(store.error).toBeNull()
  })

  it('fetchAttachments() sets errorLoad on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useTicketAttachmentsStore()
    await store.fetchAttachments('ticket-1')

    expect(store.error).toBe('errorLoad')
  })

  it('upload() prepends the created attachment on success', async () => {
    const created = makeAttachment({ id: 'new-1' })
    uploadMock.mockResolvedValue(created)

    const store = useTicketAttachmentsStore()
    const file = new File(['content'], 'notes.txt', { type: 'text/plain' })
    const result = await store.upload('ticket-1', file)

    expect(result).toEqual(created)
    expect(store.items).toEqual([created])
    expect(store.uploading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('upload() surfaces the server message on a 400', async () => {
    uploadMock.mockRejectedValue(new ApiError(400, 'File type is not allowed.'))

    const store = useTicketAttachmentsStore()
    const file = new File(['content'], 'bad.exe', { type: 'application/x-msdownload' })

    await expect(store.upload('ticket-1', file)).rejects.toBeInstanceOf(ApiError)
    expect(store.error).toBe('File type is not allowed.')
  })

  it('remove() removes the attachment from state on success', async () => {
    const attachment = makeAttachment()
    listMock.mockResolvedValue([attachment])
    deleteMock.mockResolvedValue(undefined)

    const store = useTicketAttachmentsStore()
    await store.fetchAttachments('ticket-1')
    await store.remove('ticket-1', attachment.id)

    expect(store.items).toEqual([])
    expect(store.error).toBeNull()
  })

  it('remove() sets errorDelete and rethrows on failure', async () => {
    deleteMock.mockRejectedValue(new Error('failed'))

    const store = useTicketAttachmentsStore()
    await expect(store.remove('ticket-1', '1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorDelete')
  })
})
