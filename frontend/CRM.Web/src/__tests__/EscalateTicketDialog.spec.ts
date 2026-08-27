import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import EscalateTicketDialog from '@/modules/tickets/components/EscalateTicketDialog.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type { escalateTicket } from '@/api/tickets'
import type { Ticket } from '@/types/tickets'

const { escalateTicketMock } = vi.hoisted(() => ({
  escalateTicketMock: vi.fn<typeof escalateTicket>(),
}))

vi.mock('@/api/tickets', () => ({
  escalateTicket: escalateTicketMock,
}))

function makeTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    customerEmail: 'alice@example.com',
    title: 'Cannot log in',
    description: 'Details',
    status: 'Open',
    priority: 'Normal',
    assigneeUserId: null,
    assigneeDisplayName: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    sla: {
      policyId: null,
      firstResponseDueAtUtc: null,
      resolutionDueAtUtc: null,
      firstRespondedAtUtc: null,
      resolvedAtUtc: null,
      firstResponseStatus: 'NotApplicable',
      resolutionStatus: 'NotApplicable',
      firstResponseBreachedAtUtc: null,
      resolutionBreachedAtUtc: null,
      slaLastEvaluatedAtUtc: null,
      slaAutoEscalatedAtUtc: null,
    },
    escalations: [],
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  escalateTicketMock.mockReset()
})

function mountDialog() {
  return mount(EscalateTicketDialog, {
    props: { ticketId: '1' },
    global: { plugins: [i18n] },
  })
}

describe('EscalateTicketDialog', () => {
  it('shows only the Escalate button initially', () => {
    const wrapper = mountDialog()

    expect(wrapper.find('form').exists()).toBe(false)
    expect(wrapper.find('button').exists()).toBe(true)
  })

  it('opens the reason form when Escalate is clicked', async () => {
    const wrapper = mountDialog()

    await wrapper.find('button').trigger('click')

    expect(wrapper.find('form').exists()).toBe(true)
    expect(wrapper.find('textarea').exists()).toBe(true)
  })

  it('disables submit while the reason is blank', async () => {
    const wrapper = mountDialog()
    await wrapper.find('button').trigger('click')

    const submitButton = wrapper.find('button[type="submit"]')
    expect(submitButton.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('Customer is a VIP account.')
    expect(submitButton.attributes('disabled')).toBeUndefined()
  })

  it('calls escalateTicket with the trimmed reason and closes the form on success', async () => {
    escalateTicketMock.mockResolvedValue(makeTicket({ priority: 'High' }))

    const wrapper = mountDialog()
    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('  Customer is a VIP account.  ')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(escalateTicketMock).toHaveBeenCalledWith('1', { reason: 'Customer is a VIP account.' })
    expect(wrapper.find('form').exists()).toBe(false)
  })

  it('shows the server error and keeps the form open on failure', async () => {
    escalateTicketMock.mockRejectedValue(new ApiError(400, 'Ticket is already at the highest priority.'))

    const wrapper = mountDialog()
    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('Reason')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Ticket is already at the highest priority.')
    expect(wrapper.find('form').exists()).toBe(true)
  })

  it('cancel closes the form without calling escalateTicket', async () => {
    const wrapper = mountDialog()
    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('Reason')

    const cancelButton = wrapper.findAll('button').find((b) => b.attributes('type') === 'button')!
    await cancelButton.trigger('click')

    expect(wrapper.find('form').exists()).toBe(false)
    expect(escalateTicketMock).not.toHaveBeenCalled()
  })
})
