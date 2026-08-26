import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import SlaPoliciesManagementView from '@/modules/sla/views/SlaPoliciesManagementView.vue'
import { i18n } from '@/i18n'
import type { createSlaPolicy, deleteSlaPolicy, listSlaPolicies, updateSlaPolicy } from '@/api/sla'
import type { SlaPolicy } from '@/types/tickets'

const { listMock, createMock, updateMock, deleteMock, confirmMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listSlaPolicies>(),
  createMock: vi.fn<typeof createSlaPolicy>(),
  updateMock: vi.fn<typeof updateSlaPolicy>(),
  deleteMock: vi.fn<typeof deleteSlaPolicy>(),
  confirmMock: vi.fn<() => Promise<boolean>>(),
}))

vi.mock('@/api/sla', () => ({
  listSlaPolicies: listMock,
  createSlaPolicy: createMock,
  updateSlaPolicy: updateMock,
  deleteSlaPolicy: deleteMock,
}))

vi.mock('@/composables/useConfirm', () => ({ confirm: confirmMock }))

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
  confirmMock.mockReset()
})

function mountView() {
  return mount(SlaPoliciesManagementView, {
    global: { plugins: [i18n] },
  })
}

describe('SlaPoliciesManagementView', () => {
  it('renders the list of SLA policies', async () => {
    listMock.mockResolvedValue([makeSlaPolicy({ name: 'Urgent Response Policy' })])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Urgent Response Policy')
  })

  it('renders the empty state when there are no policies', async () => {
    listMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No SLA policies yet.')
  })

  it('opens the create form and submits a new policy', async () => {
    listMock.mockResolvedValue([])
    createMock.mockResolvedValue(makeSlaPolicy({ id: 'new-1', name: 'New Policy' }))

    const wrapper = mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#sla-policy-name').setValue('New Policy')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith({
      name: 'New Policy',
      channel: null,
      priority: 'Normal',
      firstResponseMinutes: 60,
      resolutionMinutes: 480,
      isDefault: false,
      isActive: true,
    })
    expect(wrapper.text()).toContain('New Policy')
  })

  it('deletes a policy after confirmation', async () => {
    listMock.mockResolvedValue([makeSlaPolicy({ id: '1' })])
    deleteMock.mockResolvedValue(undefined)
    confirmMock.mockResolvedValue(true)

    const wrapper = mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).toHaveBeenCalledWith('1')
  })

  it('does not delete when confirmation is declined', async () => {
    listMock.mockResolvedValue([makeSlaPolicy({ id: '1' })])
    confirmMock.mockResolvedValue(false)

    const wrapper = mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteMock).not.toHaveBeenCalled()
  })

  it('opens the edit form pre-filled and submits the update', async () => {
    listMock.mockResolvedValue([makeSlaPolicy({ id: '1', name: 'Original Policy' })])
    updateMock.mockResolvedValue(makeSlaPolicy({ id: '1', name: 'Updated Policy' }))

    const wrapper = mountView()
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')

    const nameInput = wrapper.find('.sla-policy-inline-form input[type="text"]')
    expect((nameInput.element as HTMLInputElement).value).toBe('Original Policy')

    await nameInput.setValue('Updated Policy')
    await wrapper.find('.sla-policy-inline-form').trigger('submit')
    await flushPromises()

    expect(updateMock).toHaveBeenCalledWith('1', {
      name: 'Updated Policy',
      channel: null,
      priority: 'High',
      firstResponseMinutes: 30,
      resolutionMinutes: 240,
      isDefault: false,
      isActive: true,
    })
    expect(wrapper.text()).toContain('Updated Policy')
  })
})
