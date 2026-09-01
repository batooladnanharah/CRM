import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import TicketDetailsView from '@/modules/tickets/views/TicketDetailsView.vue'
import TicketsListView from '@/modules/tickets/views/TicketsListView.vue'
import { useAuthStore } from '@/stores/auth'
import { useAiStore } from '@/stores/ai'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type {
  assignTicket,
  changeTicketPriority,
  changeTicketStatus,
  createTicketMessage,
  escalateTicket,
  fetchEligibleAgents,
  fetchTicketHistory,
  getTicket,
  listTicketMessages,
} from '@/api/tickets'
import type {
  deleteTicketAttachment,
  downloadTicketAttachment,
  listTicketAttachments,
  uploadTicketAttachment,
} from '@/api/ticketAttachments'
import type { summariseTicket } from '@/api/ai'
import type { AiResponse, AiStatus } from '@/types/ai'
import type { Ticket } from '@/types/tickets'

const {
  getTicketMock,
  assignTicketMock,
  changeTicketStatusMock,
  changeTicketPriorityMock,
  fetchTicketHistoryMock,
  fetchEligibleAgentsMock,
  escalateTicketMock,
  listTicketMessagesMock,
  createTicketMessageMock,
} = vi.hoisted(() => ({
  getTicketMock: vi.fn<typeof getTicket>(),
  assignTicketMock: vi.fn<typeof assignTicket>(),
  changeTicketStatusMock: vi.fn<typeof changeTicketStatus>(),
  changeTicketPriorityMock: vi.fn<typeof changeTicketPriority>(),
  fetchTicketHistoryMock: vi.fn<typeof fetchTicketHistory>(),
  fetchEligibleAgentsMock: vi.fn<typeof fetchEligibleAgents>(),
  escalateTicketMock: vi.fn<typeof escalateTicket>(),
  listTicketMessagesMock: vi.fn<typeof listTicketMessages>(),
  createTicketMessageMock: vi.fn<typeof createTicketMessage>(),
}))

vi.mock('@/api/tickets', () => ({
  getTicket: getTicketMock,
  assignTicket: assignTicketMock,
  changeTicketStatus: changeTicketStatusMock,
  changeTicketPriority: changeTicketPriorityMock,
  fetchTicketHistory: fetchTicketHistoryMock,
  fetchEligibleAgents: fetchEligibleAgentsMock,
  escalateTicket: escalateTicketMock,
  listTicketMessages: listTicketMessagesMock,
  createTicketMessage: createTicketMessageMock,
}))

const { listTicketAttachmentsMock } = vi.hoisted(() => ({
  listTicketAttachmentsMock: vi.fn<typeof listTicketAttachments>(),
}))

vi.mock('@/api/ticketAttachments', () => ({
  listTicketAttachments: listTicketAttachmentsMock,
  uploadTicketAttachment: vi.fn<typeof uploadTicketAttachment>(),
  downloadTicketAttachment: vi.fn<typeof downloadTicketAttachment>(),
  deleteTicketAttachment: vi.fn<typeof deleteTicketAttachment>(),
}))

const { summariseTicketMock } = vi.hoisted(() => ({
  summariseTicketMock: vi.fn<typeof summariseTicket>(),
}))

vi.mock('@/api/ai', () => ({
  getAiStatus: vi.fn<typeof import('@/api/ai').getAiStatus>(),
  summariseTicket: summariseTicketMock,
}))

function makeAiResponse(overrides: Partial<AiResponse> = {}): AiResponse {
  return {
    success: true,
    content: 'Development summary: the customer cannot log in.',
    provider: 'Development',
    model: 'development-mock',
    errorCode: null,
    ...overrides,
  }
}

function setAiStatus(overrides: Partial<AiStatus> = {}) {
  const aiStore = useAiStore()
  aiStore.status = { enabled: true, provider: 'Development', available: true, ...overrides }
}

function makeTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: '1',
    customerId: 'customer-1',
    customerName: 'Alice Johnson',
    customerEmail: 'alice@example.com',
    title: 'Cannot log in',
    description: 'User reports login failures since this morning.',
    status: 'Open',
    priority: 'High',
    assigneeUserId: null,
    assigneeDisplayName: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-02T00:00:00Z',
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
    autoAssigned: false,
    ...overrides,
  }
}

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/tickets', name: 'tickets-list', component: TicketsListView },
      { path: '/tickets/:id', name: 'ticket-details', component: TicketDetailsView },
      { path: '/customers/:id', name: 'customer-profile', component: { template: '<div />' } },
    ],
  })
}

async function mountDetailsView(router: Router, id = '1') {
  router.push(`/tickets/${id}`)
  await router.isReady()

  return mount(TicketDetailsView, {
    global: {
      plugins: [router, i18n],
    },
  })
}

function makeAgent(overrides: Partial<{ id: string; displayName: string; email: string }> = {}) {
  return { id: 'agent-1', displayName: 'Second Agent', email: 'second-agent@crm.local', ...overrides }
}

function loginAsAgent() {
  const authStore = useAuthStore()
  authStore.token = 'a-valid-token'
  authStore.user = { id: 'user-1', name: 'Active Agent', email: 'agent@crm.local', roles: ['agent'] }
}

function loginAsAdmin() {
  const authStore = useAuthStore()
  authStore.token = 'a-valid-token'
  authStore.user = { id: 'admin-1', name: 'Default Admin', email: 'admin@crm.local', roles: ['admin'] }
}

beforeEach(() => {
  setActivePinia(createPinia())
  getTicketMock.mockReset()
  assignTicketMock.mockReset()
  changeTicketStatusMock.mockReset()
  changeTicketPriorityMock.mockReset()
  fetchTicketHistoryMock.mockReset()
  fetchEligibleAgentsMock.mockReset()
  fetchEligibleAgentsMock.mockResolvedValue([])
  escalateTicketMock.mockReset()
  listTicketMessagesMock.mockReset()
  listTicketMessagesMock.mockResolvedValue({ items: [], page: 1, pageSize: 100, totalCount: 0 })
  createTicketMessageMock.mockReset()
  listTicketAttachmentsMock.mockReset()
  listTicketAttachmentsMock.mockResolvedValue([])
  summariseTicketMock.mockReset()
})

describe('TicketDetailsView', () => {
  it('renders a loading state before the ticket resolves', async () => {
    let resolveGet!: (value: Ticket) => void
    getTicketMock.mockReturnValue(
      new Promise((resolve) => {
        resolveGet = resolve
      }),
    )

    const wrapper = await mountDetailsView(makeRouter())

    expect(wrapper.text()).not.toContain('Cannot log in')

    resolveGet!(makeTicket())
    await flushPromises()
  })

  it('renders the ticket title, status, priority, customer, and description', async () => {
    getTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Cannot log in')
    expect(wrapper.text()).toContain('Open')
    expect(wrapper.text()).toContain('High')
    expect(wrapper.text()).toContain('Alice Johnson')
    expect(wrapper.text()).toContain('User reports login failures since this morning.')
  })

  it('renders formatted createdAt and updatedAt timestamps', async () => {
    getTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Created')
    expect(wrapper.text()).toContain('Last updated')
  })

  it('renders the not-found fallback on a 404', async () => {
    getTicketMock.mockRejectedValue(new ApiError(404, 'Not found'))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Ticket not found.')
  })

  it('renders the error state on a generic failure', async () => {
    getTicketMock.mockRejectedValue(new Error('network down'))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Unable to load ticket information.')
  })

  it('navigates to the tickets list when Back is clicked', async () => {
    getTicketMock.mockResolvedValue(makeTicket())

    const router = makeRouter()
    const wrapper = await mountDetailsView(router)
    await flushPromises()

    const buttons = wrapper.findAll('button')
    const backButton = buttons.find((b) => b.text() === 'Back to tickets')!
    await backButton.trigger('click')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/tickets')
  })

  it('hides the assignee/status/priority controls for a non-agent user', async () => {
    getTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.findAll('select')).toHaveLength(0)
    expect(fetchEligibleAgentsMock).not.toHaveBeenCalled()
  })

  it('renders "Automatically assigned" indicator when ticket.autoAssigned && assignedAgent', async () => {
    getTicketMock.mockResolvedValue(
      makeTicket({ assigneeUserId: 'agent-1', assigneeDisplayName: 'Sara Ahmed', autoAssigned: true }),
    )

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Sara Ahmed')
    expect(wrapper.text()).toContain('Automatically assigned')
  })

  it('renders Unassigned state when assignedAgent is null', async () => {
    getTicketMock.mockResolvedValue(makeTicket({ assigneeUserId: null, assigneeDisplayName: null }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Unassigned')
    expect(wrapper.text()).not.toContain('Automatically assigned')
  })

  it('hides indicator when autoAssigned is false', async () => {
    getTicketMock.mockResolvedValue(
      makeTicket({ assigneeUserId: 'agent-1', assigneeDisplayName: 'Ahmed Hassan', autoAssigned: false }),
    )

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Ahmed Hassan')
    expect(wrapper.text()).not.toContain('Automatically assigned')
  })

  it('renders the assignee/status/priority controls for an agent user', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket())
    fetchEligibleAgentsMock.mockResolvedValue([makeAgent()])

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.findAll('select')).toHaveLength(3)
    expect(fetchEligibleAgentsMock).toHaveBeenCalled()
  })

  it('limits the status dropdown to the current status plus legal next transitions', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket({ status: 'Closed' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const statusSelect = wrapper.findAll('select')[0]!
    const options = statusSelect.findAll('option').map((o) => o.text())
    expect(options).toEqual(['Closed'])
  })

  it('calls store.assign when the assignee select changes', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket())
    fetchEligibleAgentsMock.mockResolvedValue([makeAgent()])
    assignTicketMock.mockResolvedValue(makeTicket({ assigneeUserId: 'agent-1', assigneeDisplayName: 'Second Agent' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const assigneeSelect = wrapper.findAll('select')[2]!
    await assigneeSelect.setValue('agent-1')

    expect(assignTicketMock).toHaveBeenCalledWith('1', 'agent-1')
  })

  it('calls store.changeStatus when the status select changes', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket({ status: 'Open' }))
    changeTicketStatusMock.mockResolvedValue(makeTicket({ status: 'InProgress' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const statusSelect = wrapper.findAll('select')[0]!
    await statusSelect.setValue('InProgress')

    expect(changeTicketStatusMock).toHaveBeenCalledWith('1', 'InProgress')
  })

  it('leaves the status unchanged and shows an error when changeStatus fails', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket({ status: 'Open' }))
    changeTicketStatusMock.mockRejectedValue(new ApiError(400, 'Cannot transition ticket status from Open to Closed.'))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const statusSelect = wrapper.findAll('select')[0]!
    await statusSelect.setValue('Closed')
    await flushPromises()

    expect(wrapper.text()).toContain('Cannot transition ticket status from Open to Closed.')
    expect((statusSelect.element as HTMLSelectElement).value).toBe('Open')
  })

  it('calls store.changePriority when the priority select changes', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket({ priority: 'Normal' }))
    changeTicketPriorityMock.mockResolvedValue(makeTicket({ priority: 'Urgent' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const prioritySelect = wrapper.findAll('select')[1]!
    await prioritySelect.setValue('Urgent')

    expect(changeTicketPriorityMock).toHaveBeenCalledWith('1', 'Urgent')
  })

  it('loads and renders history lazily when the panel is opened', async () => {
    getTicketMock.mockResolvedValue(makeTicket())
    fetchTicketHistoryMock.mockResolvedValue([
      {
        id: 'h1',
        changeType: 'Status',
        oldValue: 'Open',
        newValue: 'InProgress',
        reason: null,
        changedByUserId: 'user-1',
        changedByDisplayName: 'Active Agent',
        changedAtUtc: '2026-01-03T00:00:00Z',
        isSystemActor: false,
      },
    ])

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(fetchTicketHistoryMock).not.toHaveBeenCalled()

    const details = wrapper.find('details').element as HTMLDetailsElement
    details.open = true
    await wrapper.find('details').trigger('toggle')
    await flushPromises()

    expect(fetchTicketHistoryMock).toHaveBeenCalledWith('1')
    expect(wrapper.text()).toContain('Active Agent')
  })

  it('renders "Auto-escalated by SLA policy" for a system-actor escalation entry', async () => {
    getTicketMock.mockResolvedValue(makeTicket())
    fetchTicketHistoryMock.mockResolvedValue([
      {
        id: 'h1',
        changeType: 'Escalated',
        oldValue: 'Normal',
        newValue: 'High',
        reason: 'Automatically escalated due to an SLA breach.',
        changedByUserId: '00000000-0000-0000-0000-000000000000',
        changedByDisplayName: '',
        changedAtUtc: '2026-01-03T00:00:00Z',
        isSystemActor: true,
      },
    ])

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const details = wrapper.find('details').element as HTMLDetailsElement
    details.open = true
    await wrapper.find('details').trigger('toggle')
    await flushPromises()

    expect(wrapper.text()).toContain('Auto-escalated by SLA policy')
  })

  it('renders an SLA breach history entry', async () => {
    getTicketMock.mockResolvedValue(makeTicket())
    fetchTicketHistoryMock.mockResolvedValue([
      {
        id: 'h1',
        changeType: 'SlaBreached',
        oldValue: null,
        newValue: 'FirstResponse',
        reason: null,
        changedByUserId: '00000000-0000-0000-0000-000000000000',
        changedByDisplayName: '',
        changedAtUtc: '2026-01-03T00:00:00Z',
        isSystemActor: true,
      },
    ])

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const details = wrapper.find('details').element as HTMLDetailsElement
    details.open = true
    await wrapper.find('details').trigger('toggle')
    await flushPromises()

    expect(wrapper.text()).toContain('SLA breached')
  })

  it('shows the Breached SLA badge from persisted ticket fields without recomputing locally', async () => {
    getTicketMock.mockResolvedValue(
      makeTicket({
        sla: {
          policyId: 'policy-1',
          firstResponseDueAtUtc: '2026-01-01T01:00:00Z',
          resolutionDueAtUtc: '2026-01-01T08:00:00Z',
          firstRespondedAtUtc: null,
          resolvedAtUtc: null,
          firstResponseStatus: 'Breached',
          resolutionStatus: 'AtRisk',
          firstResponseBreachedAtUtc: '2026-01-01T01:05:00Z',
          resolutionBreachedAtUtc: null,
          slaLastEvaluatedAtUtc: '2026-01-01T01:05:00Z',
          slaAutoEscalatedAtUtc: '2026-01-01T01:05:00Z',
        },
      }),
    )

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('Breached')
  })

  it('renders the Messages and Attachments sections', async () => {
    getTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.findComponent({ name: 'TicketMessagesSection' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'TicketAttachmentsSection' }).exists()).toBe(true)
    expect(listTicketMessagesMock).toHaveBeenCalledWith('1', 1, 100)
    expect(listTicketAttachmentsMock).toHaveBeenCalledWith('1')
  })

  it('hides the Escalate button for a non-admin user', async () => {
    loginAsAgent()
    getTicketMock.mockResolvedValue(makeTicket({ status: 'Open' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.findComponent({ name: 'EscalateTicketDialog' }).exists()).toBe(false)
  })

  it('hides the Escalate button for an admin when the ticket is resolved or closed', async () => {
    loginAsAdmin()
    getTicketMock.mockResolvedValue(makeTicket({ status: 'Closed' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.findComponent({ name: 'EscalateTicketDialog' }).exists()).toBe(false)
  })

  it('shows the Escalate button for an admin when the ticket is not terminal', async () => {
    loginAsAdmin()
    getTicketMock.mockResolvedValue(makeTicket({ status: 'Open' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.findComponent({ name: 'EscalateTicketDialog' }).exists()).toBe(true)
  })
})

describe('TicketDetailsView AI Assistance panel', () => {
  it('renders the Generate Summary button when AI is available', async () => {
    setAiStatus()
    getTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('AI Assistance')
    expect(wrapper.text()).toContain('Generate Summary')
  })

  it('clicking Generate Summary triggers the store action and displays the returned summary and Regenerate', async () => {
    setAiStatus()
    getTicketMock.mockResolvedValue(makeTicket())
    summariseTicketMock.mockResolvedValue(makeAiResponse({ content: 'Development summary: login issue.' }))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const generateButton = wrapper.findAll('button').find((b) => b.text() === 'Generate Summary')!
    await generateButton.trigger('click')
    await flushPromises()

    expect(summariseTicketMock).toHaveBeenCalledWith('1')
    expect(wrapper.text()).toContain('Development summary: login issue.')
    expect(wrapper.text()).toContain('Generated by AI')
    expect(wrapper.text()).toContain('Regenerate')
  })

  it('shows the loading state while the summary request is in flight', async () => {
    setAiStatus()
    getTicketMock.mockResolvedValue(makeTicket())
    let resolveSummary!: (value: AiResponse) => void
    summariseTicketMock.mockReturnValue(
      new Promise((resolve) => {
        resolveSummary = resolve
      }),
    )

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const generateButton = wrapper.findAll('button').find((b) => b.text() === 'Generate Summary')!
    await generateButton.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Generating summary…')

    resolveSummary!(makeAiResponse())
    await flushPromises()
  })

  it('shows the error state with Try Again when the summary request fails', async () => {
    setAiStatus()
    getTicketMock.mockResolvedValue(makeTicket())
    summariseTicketMock.mockRejectedValue(new ApiError(502, 'Provider failed'))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const generateButton = wrapper.findAll('button').find((b) => b.text() === 'Generate Summary')!
    await generateButton.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('AI summary could not be generated.')
    expect(wrapper.text()).toContain('Try Again')
  })

  it('disables Generate Summary while a request is loading', async () => {
    setAiStatus()
    getTicketMock.mockResolvedValue(makeTicket())
    summariseTicketMock.mockReturnValue(new Promise(() => {}))

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    const generateButton = wrapper.findAll('button').find((b) => b.text() === 'Generate Summary')!
    await generateButton.trigger('click')
    await flushPromises()

    expect(generateButton.attributes('disabled')).toBeDefined()
  })

  it('renders the unavailable state and hides the buttons when AI status is not available', async () => {
    setAiStatus({ enabled: false, provider: null, available: false })
    getTicketMock.mockResolvedValue(makeTicket())

    const wrapper = await mountDetailsView(makeRouter())
    await flushPromises()

    expect(wrapper.text()).toContain('AI assistance is not configured.')
    expect(wrapper.findAll('button').find((b) => b.text() === 'Generate Summary')).toBeUndefined()
    expect(summariseTicketMock).not.toHaveBeenCalled()
  })

  it('does not call the summary API automatically on mount', async () => {
    setAiStatus()
    getTicketMock.mockResolvedValue(makeTicket())

    await mountDetailsView(makeRouter())
    await flushPromises()

    expect(summariseTicketMock).not.toHaveBeenCalled()
  })
})
