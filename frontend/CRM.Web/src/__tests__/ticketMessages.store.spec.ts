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
    channel: 'Web',
    emailDeliveryStatus: null,
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
    expect(createMock).toHaveBeenCalledWith('ticket-1', {
      body: 'New message',
      isInternal: true,
      mentionedUserIds: undefined,
      channel: 'Web',
      subjectOverride: undefined,
    })
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
      channel: 'Web',
      subjectOverride: undefined,
    })
    expect(store.items[0]).toEqual(created)
  })

  it('addMessage() sends channel=Email and derived subject, tracked via sendError on failure', async () => {
    const created = makeMessage({ id: 'new-3', channel: 'Email', emailDeliveryStatus: 'Sent' })
    createMock.mockResolvedValue(created)

    const store = useTicketMessagesStore()
    await store.addMessage('ticket-1', 'Following up by email', false, undefined, 'Email', 'Re: Cannot log in')

    expect(createMock).toHaveBeenCalledWith('ticket-1', {
      body: 'Following up by email',
      isInternal: false,
      mentionedUserIds: undefined,
      channel: 'Email',
      subjectOverride: 'Re: Cannot log in',
    })
    expect(store.sendError).toBeNull()
  })

  it('addMessage() email failure sets sendError but does not clear content and does not set error', async () => {
    createMock.mockRejectedValue(new Error('email delivery failed'))

    const store = useTicketMessagesStore()
    await expect(
      store.addMessage('ticket-1', 'Following up by email', false, undefined, 'Email', 'Re: Cannot log in'),
    ).rejects.toThrow('email delivery failed')

    expect(store.sendError).toBe('errorSave')
    expect(store.error).toBeNull()
  })
})
