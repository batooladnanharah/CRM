import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCommunicationChannelsStore } from '@/stores/communicationChannels'
import type {
  createChannel,
  deleteChannel,
  ingestChannelEmail,
  listChannelEmails,
  listChannels,
  updateChannel,
} from '@/api/communicationChannels'
import type { Channel, EmailMessage } from '@/types/communicationChannels'

const { listMock, createMock, updateMock, deleteMock, listEmailsMock, ingestMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listChannels>(),
  createMock: vi.fn<typeof createChannel>(),
  updateMock: vi.fn<typeof updateChannel>(),
  deleteMock: vi.fn<typeof deleteChannel>(),
  listEmailsMock: vi.fn<typeof listChannelEmails>(),
  ingestMock: vi.fn<typeof ingestChannelEmail>(),
}))

vi.mock('@/api/communicationChannels', () => ({
  listChannels: listMock,
  createChannel: createMock,
  updateChannel: updateMock,
  deleteChannel: deleteMock,
  listChannelEmails: listEmailsMock,
  ingestChannelEmail: ingestMock,
}))

function makeChannel(overrides: Partial<Channel> = {}): Channel {
  return {
    id: '1',
    name: 'Support Inbox',
    type: 'Email',
    isEnabled: true,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeEmail(overrides: Partial<EmailMessage> = {}): EmailMessage {
  return {
    id: 'e1',
    channelId: '1',
    fromAddress: 'customer@example.com',
    toAddress: 'support@example.com',
    subject: 'Help',
    body: 'I need help.',
    receivedAtUtc: '2026-01-01T00:00:00Z',
    ticketId: null,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listMock.mockReset()
  createMock.mockReset()
  updateMock.mockReset()
  deleteMock.mockReset()
  listEmailsMock.mockReset()
  ingestMock.mockReset()
})

describe('communicationChannels store', () => {
  it('has the expected initial state', () => {
    const store = useCommunicationChannelsStore()

    expect(store.channels).toEqual([])
    expect(store.selectedChannelId).toBeNull()
    expect(store.emails).toEqual([])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchChannels() populates channels on success', async () => {
    const channel = makeChannel()
    listMock.mockResolvedValue([channel])

    const store = useCommunicationChannelsStore()
    await store.fetchChannels()

    expect(store.channels).toEqual([channel])
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchChannels() sets errorLoad and does not throw on failure', async () => {
    listMock.mockRejectedValue(new Error('network down'))

    const store = useCommunicationChannelsStore()
    await expect(store.fetchChannels()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.channels).toEqual([])
  })

  it('create() appends the created channel, sorted by name', async () => {
    listMock.mockResolvedValue([])
    const store = useCommunicationChannelsStore()
    await store.fetchChannels()

    createMock.mockResolvedValue(makeChannel({ id: '2', name: 'Apple Inbox' }))
    const result = await store.create({ name: 'Apple Inbox', type: 'Email' })

    expect(result.name).toBe('Apple Inbox')
    expect(store.channels.map((c) => c.name)).toEqual(['Apple Inbox'])
    expect(store.saving).toBe(false)
    expect(store.error).toBeNull()
  })

  it('create() sets errorSave and rethrows on failure', async () => {
    createMock.mockRejectedValue(new Error('failed'))

    const store = useCommunicationChannelsStore()
    await expect(store.create({ name: 'X', type: 'Email' })).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.saving).toBe(false)
  })

  it('update() replaces the channel in place on success', async () => {
    listMock.mockResolvedValue([makeChannel({ id: '1', name: 'Original' })])
    const store = useCommunicationChannelsStore()
    await store.fetchChannels()

    const updated = makeChannel({ id: '1', name: 'Renamed', isEnabled: false })
    updateMock.mockResolvedValue(updated)
    await store.update('1', { name: 'Renamed', isEnabled: false })

    expect(store.channels[0]).toEqual(updated)
  })

  it('remove() removes the channel from state on success', async () => {
    listMock.mockResolvedValue([makeChannel({ id: '1' })])
    const store = useCommunicationChannelsStore()
    await store.fetchChannels()

    deleteMock.mockResolvedValue(undefined)
    await store.remove('1')

    expect(store.channels).toEqual([])
  })

  it('remove() sets errorDelete and rethrows on failure', async () => {
    deleteMock.mockRejectedValue(new Error('failed'))

    const store = useCommunicationChannelsStore()
    await expect(store.remove('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorDelete')
  })

  it('selectChannel() loads emails for the given channel', async () => {
    const email = makeEmail()
    listEmailsMock.mockResolvedValue([email])

    const store = useCommunicationChannelsStore()
    await store.selectChannel('1')

    expect(store.selectedChannelId).toBe('1')
    expect(store.emails).toEqual([email])
    expect(store.emailsLoading).toBe(false)
    expect(listEmailsMock).toHaveBeenCalledWith('1')
  })

  it('selectChannel() sets errorLoadEmails on failure', async () => {
    listEmailsMock.mockRejectedValue(new Error('network down'))

    const store = useCommunicationChannelsStore()
    await store.selectChannel('1')

    expect(store.error).toBe('errorLoadEmails')
    expect(store.emails).toEqual([])
  })

  it('ingestEmail() prepends the new email when the ingested channel is selected', async () => {
    listEmailsMock.mockResolvedValue([])
    const store = useCommunicationChannelsStore()
    await store.selectChannel('1')

    const created = makeEmail({ id: 'e2', subject: 'New message' })
    ingestMock.mockResolvedValue(created)
    await store.ingestEmail('1', {
      fromAddress: 'customer@example.com',
      toAddress: 'support@example.com',
      subject: 'New message',
      body: 'Body',
    })

    expect(store.emails).toEqual([created])
  })

  it('ingestEmail() does not touch emails when a different channel is selected', async () => {
    listEmailsMock.mockResolvedValue([])
    const store = useCommunicationChannelsStore()
    await store.selectChannel('1')

    ingestMock.mockResolvedValue(makeEmail({ id: 'e2', channelId: '2' }))
    await store.ingestEmail('2', {
      fromAddress: 'customer@example.com',
      toAddress: 'support@example.com',
      subject: 'New message',
      body: 'Body',
    })

    expect(store.emails).toEqual([])
  })

  it('ingestEmail() sets errorIngest and rethrows on failure', async () => {
    ingestMock.mockRejectedValue(new Error('failed'))

    const store = useCommunicationChannelsStore()
    await expect(
      store.ingestEmail('1', {
        fromAddress: 'a@example.com',
        toAddress: 'b@example.com',
        subject: 'S',
        body: 'B',
      }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorIngest')
  })
})
