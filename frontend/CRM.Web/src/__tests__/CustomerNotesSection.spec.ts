import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import CustomerNotesSection from '@/modules/customers/components/CustomerNotesSection.vue'
import { useAuthStore } from '@/stores/auth'
import { i18n } from '@/i18n'
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
  vi.restoreAllMocks()
})

function mountSection(userId = 'author-1', roles: string[] = ['agent']) {
  const authStore = useAuthStore()
  authStore.user = { id: userId, name: 'Test User', email: 'test@crm.local', roles }

  return mount(CustomerNotesSection, {
    props: { customerId: 'customer-1' },
    global: { plugins: [i18n] },
  })
}

describe('CustomerNotesSection', () => {
  it('shows a loading skeleton before the promise resolves', async () => {
    let resolveFetch!: (value: CustomerNote[]) => void
    listMock.mockReturnValue(
      new Promise((resolve) => {
        resolveFetch = resolve
      }),
    )

    const wrapper = mountSection()
    await nextTick()

    expect(wrapper.find('.skeleton').exists()).toBe(true)

    resolveFetch!([])
    await flushPromises()
  })

  it('renders the empty state when there are no notes', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountSection()
    await flushPromises()

    expect(wrapper.text()).toContain('No notes yet.')
  })

  it('renders notes newest-first as returned by the store', async () => {
    listMock.mockResolvedValue([
      makeNote({ id: '2', content: 'Newer', createdAtUtc: '2026-01-02T00:00:00Z' }),
      makeNote({ id: '1', content: 'Older', createdAtUtc: '2026-01-01T00:00:00Z' }),
    ])

    const wrapper = mountSection()
    await flushPromises()

    const items = wrapper.findAll('.notes-list > li')
    expect(items[0]!.text()).toContain('Newer')
    expect(items[1]!.text()).toContain('Older')
  })

  it('opens the add form and creates a note on submit', async () => {
    listMock.mockResolvedValue([])
    const created = makeNote({ id: 'new-1', content: 'Hello there' })
    createMock.mockResolvedValue(created)

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('Hello there')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith('customer-1', { content: 'Hello there' })
    expect(wrapper.text()).toContain('Hello there')
  })

  it('disables Save while saving or when content is blank', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    const saveButton = wrapper
      .findAll('.note-form button')
      .find((b) => b.attributes('type') === 'submit')!
    expect(saveButton.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('   ')
    expect(saveButton.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('Real content')
    expect(saveButton.attributes('disabled')).toBeUndefined()
  })

  it('shows Edit/Delete only for the note author or an admin', async () => {
    listMock.mockResolvedValue([
      makeNote({ id: '1', authorId: 'someone-else' }),
    ])

    const wrapper = mountSection('author-1', ['agent'])
    await flushPromises()

    expect(wrapper.find('.note-actions').exists()).toBe(false)
  })

  it('shows Edit/Delete for the note author', async () => {
    listMock.mockResolvedValue([makeNote({ id: '1', authorId: 'author-1' })])

    const wrapper = mountSection('author-1', ['agent'])
    await flushPromises()

    expect(wrapper.find('.note-actions').exists()).toBe(true)
  })

  it('shows Edit/Delete for an admin regardless of authorship', async () => {
    listMock.mockResolvedValue([makeNote({ id: '1', authorId: 'someone-else' })])

    const wrapper = mountSection('admin-1', ['admin'])
    await flushPromises()

    expect(wrapper.find('.note-actions').exists()).toBe(true)
  })

  it('edits a note via the inline form', async () => {
    listMock.mockResolvedValue([makeNote({ id: '1', authorId: 'author-1', content: 'Original' })])
    const updated = makeNote({ id: '1', authorId: 'author-1', content: 'Changed' })
    updateMock.mockResolvedValue(updated)

    const wrapper = mountSection('author-1', ['agent'])
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')
    await wrapper.find('textarea').setValue('Changed')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith('customer-1', '1', { content: 'Changed' })
    expect(wrapper.text()).toContain('Changed')
  })

  it('deletes a note after confirmation', async () => {
    listMock.mockResolvedValue([makeNote({ id: '1', authorId: 'author-1' })])
    deleteMock.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    const wrapper = mountSection('author-1', ['agent'])
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith('customer-1', '1')
    expect(wrapper.find('.notes-list').exists()).toBe(false)
  })

  it('does not delete when confirmation is declined', async () => {
    listMock.mockResolvedValue([makeNote({ id: '1', authorId: 'author-1' })])
    vi.spyOn(window, 'confirm').mockReturnValue(false)

    const wrapper = mountSection('author-1', ['agent'])
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).not.toHaveBeenCalled()
  })

  it('surfaces a forbidden error message when edit is rejected with 403', async () => {
    listMock.mockResolvedValue([makeNote({ id: '1', authorId: 'author-1', content: 'Original' })])
    const { ApiError } = await import('@/api/http')
    updateMock.mockRejectedValue(new ApiError(403, 'Forbidden'))

    const wrapper = mountSection('author-1', ['agent'])
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')
    await wrapper.find('textarea').setValue('Changed')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('You can only edit or delete your own notes.')
  })
})
