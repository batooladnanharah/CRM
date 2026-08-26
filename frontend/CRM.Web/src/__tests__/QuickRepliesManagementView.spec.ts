import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import QuickRepliesManagementView from '@/modules/quickReplies/views/QuickRepliesManagementView.vue'
import { i18n } from '@/i18n'
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

function mountView() {
  return mount(QuickRepliesManagementView, {
    global: { plugins: [i18n] },
  })
}

describe('QuickRepliesManagementView', () => {
  it('renders the list of quick replies', async () => {
    listMock.mockResolvedValue([makeQuickReply({ title: 'Password Reset' })])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Password Reset')
  })

  it('renders the empty state when there are no quick replies', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No quick replies yet.')
  })

  it('opens the create form and submits a new quick reply', async () => {
    listMock.mockResolvedValue([])
    createMock.mockResolvedValue(makeQuickReply({ id: 'new-1', title: 'Closing' }))

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#quick-reply-title').setValue('Closing')
    await wrapper.find('#quick-reply-content').setValue('Thanks for reaching out!')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith({ title: 'Closing', content: 'Thanks for reaching out!' })
    expect(wrapper.text()).toContain('Closing')
  })

  it('deletes a quick reply after confirmation', async () => {
    listMock.mockResolvedValue([makeQuickReply({ id: '1', title: 'Greeting' })])
    deleteMock.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    const wrapper = mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith('1')
  })

  it('does not delete when confirmation is declined', async () => {
    listMock.mockResolvedValue([makeQuickReply({ id: '1', title: 'Greeting' })])
    vi.spyOn(window, 'confirm').mockReturnValue(false)

    const wrapper = mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).not.toHaveBeenCalled()
  })

  it('opens the edit form pre-filled and submits the update', async () => {
    listMock.mockResolvedValue([makeQuickReply({ id: '1', title: 'Greeting', content: 'Hi!' })])
    updateMock.mockResolvedValue(makeQuickReply({ id: '1', title: 'Updated Greeting', content: 'Hi!' }))

    const wrapper = mountView()
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')

    const titleInput = wrapper.find('.quick-reply-inline-form input[type="text"]')
    expect((titleInput.element as HTMLInputElement).value).toBe('Greeting')

    await titleInput.setValue('Updated Greeting')
    await wrapper.find('.quick-reply-inline-form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith('1', {
      title: 'Updated Greeting',
      content: 'Hi!',
      isActive: true,
    })
    expect(wrapper.text()).toContain('Updated Greeting')
  })
})
