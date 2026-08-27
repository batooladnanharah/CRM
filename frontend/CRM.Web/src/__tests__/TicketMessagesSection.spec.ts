import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import TicketMessagesSection from '@/modules/tickets/components/TicketMessagesSection.vue'
import { i18n } from '@/i18n'
import type { createTicketMessage, fetchEligibleAgents, listTicketMessages } from '@/api/tickets'
import type { createQuickReply, deleteQuickReply, listQuickReplies, updateQuickReply } from '@/api/quickReplies'
import type { PagedResult } from '@/types/customers'
import type { EligibleAgent, QuickReply, Ticket, TicketMessage } from '@/types/tickets'

const { listMock, createMock, fetchEligibleAgentsMock } = vi.hoisted(() => ({
  listMock: vi.fn<typeof listTicketMessages>(),
  createMock: vi.fn<typeof createTicketMessage>(),
  fetchEligibleAgentsMock: vi.fn<typeof fetchEligibleAgents>(),
}))

vi.mock('@/api/tickets', () => ({
  listTicketMessages: listMock,
  createTicketMessage: createMock,
  fetchEligibleAgents: fetchEligibleAgentsMock,
}))

const { listQuickRepliesMock } = vi.hoisted(() => ({
  listQuickRepliesMock: vi.fn<typeof listQuickReplies>(),
}))

vi.mock('@/api/quickReplies', () => ({
  listQuickReplies: listQuickRepliesMock,
  createQuickReply: vi.fn<typeof createQuickReply>(),
  updateQuickReply: vi.fn<typeof updateQuickReply>(),
  deleteQuickReply: vi.fn<typeof deleteQuickReply>(),
}))

function makeAgent(overrides: Partial<EligibleAgent> = {}): EligibleAgent {
  return { id: 'agent-1', displayName: 'Ahmed Hassan', email: 'ahmed@crm.local', ...overrides }
}

function makeQuickReply(overrides: Partial<QuickReply> = {}): QuickReply {
  return {
    id: 'qr-1',
    title: 'Password Reset',
    content: 'Here are the password reset steps...',
    isActive: true,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

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

function makeTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: 'ticket-1',
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

function makePage(items: TicketMessage[]): PagedResult<TicketMessage> {
  return { items, page: 1, pageSize: 100, totalCount: items.length }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listMock.mockReset()
  createMock.mockReset()
  fetchEligibleAgentsMock.mockReset()
  listQuickRepliesMock.mockReset()
  listQuickRepliesMock.mockResolvedValue([])
})

function mountSection(ticket: Ticket | null = makeTicket()) {
  return mount(TicketMessagesSection, {
    props: { ticketId: 'ticket-1', ticket },
    global: { plugins: [i18n] },
  })
}

describe('TicketMessagesSection', () => {
  it('shows a loading skeleton before the promise resolves', async () => {
    let resolveFetch!: (value: PagedResult<TicketMessage>) => void
    listMock.mockReturnValue(
      new Promise((resolve) => {
        resolveFetch = resolve
      }),
    )

    const wrapper = mountSection()
    await nextTick()

    expect(wrapper.find('.skeleton').exists()).toBe(true)

    resolveFetch!(makePage([]))
    await flushPromises()
  })

  it('renders the empty state when there are no messages', async () => {
    listMock.mockResolvedValue(makePage([]))

    const wrapper = mountSection()
    await flushPromises()

    expect(wrapper.text()).toContain('No messages yet.')
  })

  it('renders messages as returned by the store, distinguishing internal from public', async () => {
    listMock.mockResolvedValue(
      makePage([
        makeMessage({ id: '1', body: 'Public reply', isInternal: false }),
        makeMessage({ id: '2', body: 'Internal note', isInternal: true }),
      ]),
    )

    const wrapper = mountSection()
    await flushPromises()

    const items = wrapper.findAll('.messages-list > li')
    expect(items[0]!.text()).toContain('Public reply')
    expect(items[0]!.classes()).not.toContain('internal')
    expect(items[1]!.text()).toContain('Internal note')
    expect(items[1]!.classes()).toContain('internal')
  })

  it('opens the add form and posts a public message by default', async () => {
    listMock.mockResolvedValue(makePage([]))
    const created = makeMessage({ id: 'new-1', body: 'Hello there', isInternal: false })
    createMock.mockResolvedValue(created)

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('Hello there')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith('ticket-1', {
      body: 'Hello there',
      isInternal: false,
      mentionedUserIds: undefined,
      channel: 'Web',
      subjectOverride: undefined,
    })
    expect(wrapper.text()).toContain('Hello there')
  })

  it('posts an internal message when the internal-note checkbox is checked', async () => {
    listMock.mockResolvedValue(makePage([]))
    const created = makeMessage({ id: 'new-1', body: 'Internal update', isInternal: true })
    createMock.mockResolvedValue(created)

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('Internal update')
    await wrapper.find('input[type="checkbox"]').setValue(true)
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith('ticket-1', {
      body: 'Internal update',
      isInternal: true,
      mentionedUserIds: [],
      channel: 'Web',
      subjectOverride: undefined,
    })
  })

  it('disables Save while saving or when the body is blank', async () => {
    listMock.mockResolvedValue(makePage([]))

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    const saveButton = wrapper
      .findAll('.message-form button')
      .find((b) => b.attributes('type') === 'submit')!
    expect(saveButton.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('   ')
    expect(saveButton.attributes('disabled')).toBeDefined()

    await wrapper.find('textarea').setValue('Real content')
    expect(saveButton.attributes('disabled')).toBeUndefined()
  })

  it('opens the Quick Reply popover, filters by search, and inserts without sending', async () => {
    listMock.mockResolvedValue(makePage([]))
    listQuickRepliesMock.mockResolvedValue([
      makeQuickReply({ id: 'qr-1', title: 'Password Reset', content: 'Reset your password here.' }),
      makeQuickReply({ id: 'qr-2', title: 'Greeting', content: 'Hello and welcome!' }),
    ])

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    const quickReplyButton = wrapper.findAll('button').find((b) => b.text().includes('Quick Reply'))!
    await quickReplyButton.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Password Reset')
    expect(wrapper.text()).toContain('Greeting')

    await wrapper.find('.quick-reply-popover input[type="text"]').setValue('password')
    expect(wrapper.text()).toContain('Password Reset')
    expect(wrapper.text()).not.toContain('Greeting')

    await wrapper.find('.quick-reply-list li').trigger('click')

    const textarea = wrapper.find('textarea').element as HTMLTextAreaElement
    expect(textarea.value).toBe('Reset your password here.')
    expect(createMock).not.toHaveBeenCalled()
    expect(wrapper.find('.quick-reply-popover').exists()).toBe(false)

    // The textarea must remain editable after insertion.
    await wrapper.find('textarea').setValue('Reset your password here. Extra note.')
    expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe(
      'Reset your password here. Extra note.',
    )
  })

  it('shows the Quick Reply loading and empty states', async () => {
    listMock.mockResolvedValue(makePage([]))
    let resolveQuickReplies!: (value: QuickReply[]) => void
    listQuickRepliesMock.mockReturnValue(
      new Promise((resolve) => {
        resolveQuickReplies = resolve
      }),
    )

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    const quickReplyButton = wrapper.findAll('button').find((b) => b.text().includes('Quick Reply'))!
    await quickReplyButton.trigger('click')
    await nextTick()

    expect(wrapper.text()).toContain('Loading quick replies')

    resolveQuickReplies!([])
    await flushPromises()

    expect(wrapper.text()).toContain('No quick replies yet.')
  })

  it('opens a mention dropdown while typing "@" in Internal Note mode and forwards the id on submit', async () => {
    listMock.mockResolvedValue(makePage([]))
    fetchEligibleAgentsMock.mockResolvedValue([makeAgent({ id: 'agent-1', displayName: 'Ahmed Hassan' })])
    createMock.mockResolvedValue(makeMessage({ id: 'new-1', isInternal: true }))

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('input[type="checkbox"]').setValue(true)
    await wrapper.find('textarea').setValue('Please look at this @Ah')
    await flushPromises()

    expect(wrapper.text()).toContain('Ahmed Hassan')

    await wrapper.find('.mention-option').trigger('click')

    const textarea = wrapper.find('textarea').element as HTMLTextAreaElement
    expect(textarea.value).toContain('@Ahmed Hassan')
    expect(wrapper.text()).toContain('Ahmed Hassan')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createMock).toHaveBeenCalledWith('ticket-1', expect.objectContaining({
      isInternal: true,
      mentionedUserIds: ['agent-1'],
    }))
  })

  it('does not show mention dropdown or chips when not in Internal Note mode', async () => {
    listMock.mockResolvedValue(makePage([]))

    const wrapper = mountSection()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('textarea').setValue('Hello @Ah')
    await flushPromises()

    expect(wrapper.find('.mention-dropdown').exists()).toBe(false)
    expect(fetchEligibleAgentsMock).not.toHaveBeenCalled()
  })
})

describe('TicketMessagesSection email reply', () => {
  it('renders the Reply Via selector with Web and Email options', async () => {
    listMock.mockResolvedValue(makePage([]))

    const wrapper = mountSection()
    await flushPromises()
    await wrapper.find('button').trigger('click')

    const options = wrapper.find('#reply-via-select').findAll('option')
    expect(options.map((o) => o.element.value)).toEqual(['Web', 'Email'])
  })

  it('shows read-only To with the customer email and prefills subject with Re: {ticket.subject} when Email is selected', async () => {
    listMock.mockResolvedValue(makePage([]))

    const wrapper = mountSection()
    await flushPromises()
    await wrapper.find('button').trigger('click')
    await wrapper.find('#reply-via-select').setValue('Email')

    expect(wrapper.find('.email-to-line').text()).toContain('alice@example.com')
    const subjectInput = wrapper.find('.ui-input-field input')
    expect((subjectInput.element as HTMLInputElement).value).toBe('Re: Cannot log in')
  })

  it('disables Send when customer has no email', async () => {
    listMock.mockResolvedValue(makePage([]))

    const wrapper = mountSection(makeTicket({ customerEmail: '' }))
    await flushPromises()
    await wrapper.find('button').trigger('click')
    await wrapper.find('#reply-via-select').setValue('Email')
    await wrapper.find('textarea').setValue('Following up')

    const saveButton = wrapper
      .findAll('.message-form button')
      .find((b) => b.attributes('type') === 'submit')!
    expect(saveButton.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('This customer has no email address on file.')
  })

  it('calls createTicketMessage with channel=Email and the derived subject, showing loading state while sending', async () => {
    listMock.mockResolvedValue(makePage([]))
    let resolveCreate!: (value: TicketMessage) => void
    createMock.mockReturnValue(
      new Promise((resolve) => {
        resolveCreate = resolve
      }),
    )

    const wrapper = mountSection()
    await flushPromises()
    await wrapper.find('button').trigger('click')
    await wrapper.find('#reply-via-select').setValue('Email')
    await wrapper.find('textarea').setValue('Following up by email')
    await wrapper.find('form').trigger('submit')
    await nextTick()

    expect(wrapper.text()).toContain('Sending…')
    expect(createMock).toHaveBeenCalledWith('ticket-1', {
      body: 'Following up by email',
      isInternal: false,
      mentionedUserIds: undefined,
      channel: 'Email',
      subjectOverride: 'Re: Cannot log in',
    })

    resolveCreate!(makeMessage({ id: 'new-1', channel: 'Email', emailDeliveryStatus: 'Sent' }))
    await flushPromises()
  })

  it('on failure, keeps the draft content and shows the failure alert', async () => {
    listMock.mockResolvedValue(makePage([]))
    createMock.mockRejectedValue(new Error('email delivery failed'))

    const wrapper = mountSection()
    await flushPromises()
    await wrapper.find('button').trigger('click')
    await wrapper.find('#reply-via-select').setValue('Email')
    await wrapper.find('textarea').setValue('Following up by email')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe('Following up by email')
    expect(wrapper.text()).toContain('Unable to send email. Please try again.')
  })
})
