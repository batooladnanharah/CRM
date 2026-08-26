import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import CommunicationChannelsManagementView from '@/modules/communicationChannels/views/CommunicationChannelsManagementView.vue'
import { i18n } from '@/i18n'
import type {
  createChannel,
  deleteChannel,
  ingestChannelEmail,
  listChannelEmails,
  listChannels,
  updateChannel,
} from '@/api/communicationChannels'
import type { Channel, EmailMessage } from '@/types/communicationChannels'

const { listMock, createMock, updateMock, deleteMock, listEmailsMock, confirmMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listChannels>(),
  createMock: vi.fn<typeof createChannel>(),
  updateMock: vi.fn<typeof updateChannel>(),
  deleteMock: vi.fn<typeof deleteChannel>(),
  listEmailsMock: vi.fn<typeof listChannelEmails>(),
  confirmMock: vi.fn<() => Promise<boolean>>(),
}))

vi.mock('@/api/communicationChannels', () => ({
  listChannels: listMock,
  createChannel: createMock,
  updateChannel: updateMock,
  deleteChannel: deleteMock,
  listChannelEmails: listEmailsMock,
  ingestChannelEmail: vi.fn<typeof ingestChannelEmail>(),
}))

vi.mock('@/composables/useConfirm', () => ({ confirm: confirmMock }))

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
  confirmMock.mockReset()
})

function mountView() {
  return mount(CommunicationChannelsManagementView, {
    global: { plugins: [i18n] },
  })
}

describe('CommunicationChannelsManagementView', () => {
  it('renders the empty state when there are no channels', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No channels yet.')
  })

  it('submits the new channel form', async () => {
    listMock.mockResolvedValue([])
    createMock.mockResolvedValue(makeChannel({ id: 'new-1', name: 'Sales Inbox' }))

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#channel-name').setValue('Sales Inbox')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith({ name: 'Sales Inbox', type: 'Email' })
    expect(wrapper.text()).toContain('Sales Inbox')
  })

  it('selects a channel and renders its email list', async () => {
    listMock.mockResolvedValue([makeChannel({ id: '1', name: 'Support Inbox' })])
    listEmailsMock.mockResolvedValue([makeEmail({ subject: 'Password reset' })])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Select a channel to see its recent emails.')

    await wrapper.find('.channel-select').trigger('click')
    await flushPromises()

    expect(listEmailsMock).toHaveBeenCalledWith('1')
    expect(wrapper.text()).toContain('Password reset')
  })

  it('shows the empty emails state for a channel with no emails', async () => {
    listMock.mockResolvedValue([makeChannel({ id: '1' })])
    listEmailsMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('.channel-select').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('No emails received yet.')
  })

  it('deletes a channel after confirmation', async () => {
    listMock.mockResolvedValue([makeChannel({ id: '1', name: 'Deletable Inbox' })])
    deleteMock.mockResolvedValue(undefined)
    confirmMock.mockResolvedValue(true)

    const wrapper = mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith('1')
  })

  it('toggles enabled/disabled for a channel', async () => {
    listMock.mockResolvedValue([makeChannel({ id: '1', name: 'Toggle Inbox', isEnabled: true })])
    updateMock.mockResolvedValue(makeChannel({ id: '1', name: 'Toggle Inbox', isEnabled: false }))

    const wrapper = mountView()
    await flushPromises()

    const disableButton = wrapper.findAll('button').find((b) => b.text() === 'Disable')!
    await disableButton.trigger('click')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith('1', { name: 'Toggle Inbox', isEnabled: false })
  })
})
