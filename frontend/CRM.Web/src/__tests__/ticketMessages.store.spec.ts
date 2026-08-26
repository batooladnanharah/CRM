import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useTicketMessagesStore } from '@/stores/ticketMessages'
import type { createTicketMessage, listTicketMessages } from '@/api/tickets'
import type { PagedResult } from '@/types/customers'
import type { TicketMessage } from '@/types/tickets'

const { listMock, createMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listTicketMessages>(),
  createMock: vi.fn<typeof createTicketMessage>(),
}))

vi.mock('@/api/tickets', () => ({
  listTicketMessages: listMock,
  createTicketMessage: createMock,
}))

function makeMessage(overrides: Partial<TicketMessage> = {}): TicketMessage {
  return {
    id: '1',
    ticketId: 'ticket-1',
    authorUserId: 'author-1',
    authorDisplayName: 'Active Agent',
    body: 'Original message',
    isInternal: false,
    mentionedUserIds: [],
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makePage(
  items: TicketMessage[],
  overrides: Partial<PagedResult<TicketMessage>> = {},
): PagedResult<TicketMessage> {
  return { items, page: 1, pageSize: 100, totalCount: items.length, ...overrides }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listMock.mockReset()
  createMock.mockReset()
})

describe('ticketMessages store', () => {
  it('has the expected initial state', () => {
    const store = useTicketMessagesStore()

    expect(store.items).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchMessages() populates items on success', async () => {
    const message = makeMessage()
    listMock.mockResolvedValue(makePage([message]))

    const store = useTicketMessagesStore()
    await store.fetchMessages('ticket-1')

    expect(store.items).toEqual([message])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchMessages() sets errorLoad and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useTicketMessagesStore()
    await expect(store.fetchMessages('ticket-1')).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.items).toEqual([])
  })

  it('addMessage() prepends the created message on success', async () => {
    const created = makeMessage({ id: 'new-1', body: 'New message', isInternal: true })
    createMock.mockResolvedValue(created)

    const store = useTicketMessagesStore()
    const result = await store.addMessage('ticket-1', 'New message', true)

    expect(result).toEqual(created)
    expect(store.items).toEqual([created])
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
    expect(createMock).toHaveBeenCalledWith('ticket-1', { body: 'New message', isInternal: true })
  })

  it('addMessage() sets errorSave and rethrows on failure', async () => {
    createMock.mockRejectedValue(new Error('failed'))

    const store = useTicketMessagesStore()
    await expect(store.addMessage('ticket-1', 'New message', false)).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.saving).toBe(false)
  })

  it('addMessage() forwards mentionedUserIds when provided', async () => {
    const created = makeMessage({
      id: 'new-2',
      body: 'Please review',
      isInternal: true,
      mentionedUserIds: ['agent-1', 'agent-2'],
    })
    createMock.mockResolvedValue(created)

    const store = useTicketMessagesStore()
    await store.addMessage('ticket-1', 'Please review', true, ['agent-1', 'agent-2'])

    expect(createMock).toHaveBeenCalledWith('ticket-1', {
      body: 'Please review',
      isInternal: true,
      mentionedUserIds: ['agent-1', 'agent-2'],
    })
    expect(store.items[0]).toEqual(created)
  })
})
