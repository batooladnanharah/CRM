import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalTicketDetailsView from '@/modules/customerPortal/views/PortalTicketDetailsView.vue'
import { i18n } from '@/i18n'
import type { CustomerTicketDetails } from '@/types/customerPortal'

function makeTicketDetails(overrides: Partial<CustomerTicketDetails> = {}): CustomerTicketDetails {
  return {
    id: '1',
    title: 'Cannot log in',
    description: 'Login fails since this morning.',
    status: 'Open',
    priority: 'High',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-02T00:00:00Z',
    messages: [],
    history: [],
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    currentTicket: null as CustomerTicketDetails | null,
    loading: false,
    error: null as string | null,
    notFound: false,
    sendingReply: false,
    replyError: null as string | null,
    fetchTicket: vi.fn<() => Promise<void>>(),
    refreshTicket: vi.fn<() => Promise<void>>(),
    sendReply: vi.fn<() => Promise<unknown>>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/customerPortal', () => ({
  useCustomerPortalStore: () => fakeStore,
}))

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/portal/tickets', name: 'portal-tickets-list', component: { template: '<div />' } },
      { path: '/portal/tickets/new', name: 'portal-ticket-create', component: { template: '<div />' } },
      { path: '/portal/tickets/:id', name: 'portal-ticket-details', component: PortalTicketDetailsView },
    ],
  })
}

async function mountView(path = '/portal/tickets/1', router: Router = makeRouter()) {
  router.push(path)
  await router.isReady()

  return mount(PortalTicketDetailsView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('PortalTicketDetailsView', () => {
  it('calls fetchTicket on mount with the route id', async () => {
    await mountView('/portal/tickets/ticket-42')

    expect(fakeStore.fetchTicket).toHaveBeenCalledWith('ticket-42')
  })

  it('shows the loading state', async () => {
    fakeStore = makeFakeStore({ loading: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading your tickets')
  })

  it('shows ticket details once loaded', async () => {
    fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Cannot log in')
    expect(wrapper.text()).toContain('Login fails since this morning.')
  })

  it('renders customer-visible messages with sender labels, in order', async () => {
    fakeStore = makeFakeStore({
      currentTicket: makeTicketDetails({
        messages: [
          { id: 'm1', senderType: 'Customer', body: 'I still need help.', createdAtUtc: '2026-01-01T12:00:00Z' },
          { id: 'm2', senderType: 'Agent', body: 'We are looking into this.', createdAtUtc: '2026-01-01T13:00:00Z' },
        ],
      }),
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('We are looking into this.')
    expect(wrapper.text()).toContain('I still need help.')
    // Messages render in the order the store already returns them (server
    // orders by CreatedAtUtc ascending) — the view does not re-sort, only
    // labels each with its sender.
    const bodyIndex = wrapper.text().indexOf('I still need help.')
    const agentIndex = wrapper.text().indexOf('We are looking into this.')
    expect(bodyIndex).toBeLessThan(agentIndex)
  })

  // GET /api/customer/tickets/{id} already filters IsInternal messages out
  // server-side, and CustomerTicketMessage (the frontend type) carries no
  // internal flag at all — there is nothing for the view to leak even if a
  // caller injected one, so this guards the type contract instead of a
  // runtime check: any message present in the store is renderable as-is.
  it('renders every message the store returns (server already excludes internal notes)', async () => {
    fakeStore = makeFakeStore({
      currentTicket: makeTicketDetails({
        messages: [{ id: 'm1', senderType: 'Agent', body: 'Public reply only.', createdAtUtc: '2026-01-01T12:00:00Z' }],
      }),
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Public reply only.')
    expect(wrapper.findAll('li').length).toBe(1)
  })

  it('shows the no-messages placeholder when there are none', async () => {
    fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('No replies yet.')
  })

  it('shows the success banner when navigated with submitted=1', async () => {
    fakeStore = makeFakeStore({ currentTicket: makeTicketDetails({ id: 'ticket-99' }) })
    const router = makeRouter()
    router.push('/portal/tickets/ticket-99?submitted=1')
    await router.isReady()
    const wrapper = mount(PortalTicketDetailsView, { global: { plugins: [router, i18n] } })
    await flushPromises()

    expect(wrapper.text()).toContain('Your ticket has been submitted successfully. Ticket #ticket-99')
  })

  it('shows the error state with a retry action', async () => {
    fakeStore = makeFakeStore({ error: 'errorLoad' })
    const wrapper = await mountView()

    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')

    expect(fakeStore.fetchTicket).toHaveBeenCalledTimes(2)
  })

  it('shows the not-found state when the store reports notFound', async () => {
    fakeStore = makeFakeStore({ notFound: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain("We couldn't find that ticket.")
    expect(wrapper.find('textarea').exists()).toBe(false)
  })

  describe('composer', () => {
    it('disables send when the draft is empty', async () => {
      fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
      const wrapper = await mountView()

      const sendButton = wrapper.findAll('button').find((b) => b.text() === 'Send')!
      expect(sendButton.attributes('disabled')).toBeDefined()
    })

    it('enables send once the draft has non-whitespace content, and disables while sendingReply', async () => {
      fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
      const wrapper = await mountView()

      await wrapper.find('textarea').setValue('Here is more detail.')
      let sendButton = wrapper.findAll('button').find((b) => b.text() === 'Send')!
      expect(sendButton.attributes('disabled')).toBeUndefined()

      fakeStore.sendingReply = true
      await wrapper.vm.$nextTick()
      sendButton = wrapper.findAll('button').find((b) => b.text() === 'Sending…')!
      expect(sendButton.attributes('disabled')).toBeDefined()
    })

    it('calls store.sendReply with the trimmed draft and clears it on success', async () => {
      fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
      fakeStore.sendReply = vi.fn<() => Promise<unknown>>().mockResolvedValue({
        id: 'm2', senderType: 'Customer', body: 'More detail.', createdAtUtc: '2026-01-03T00:00:00Z',
      })
      const wrapper = await mountView()

      await wrapper.find('textarea').setValue('  More detail.  ')
      await wrapper.find('form.composer').trigger('submit')
      await flushPromises()

      expect(fakeStore.sendReply).toHaveBeenCalledWith('1', 'More detail.')
      expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe('')
    })

    it('keeps the draft content in the composer when sendReply fails', async () => {
      fakeStore = makeFakeStore({ currentTicket: makeTicketDetails() })
      fakeStore.sendReply = vi.fn<() => Promise<unknown>>().mockRejectedValue(new Error('failed'))
      const wrapper = await mountView()

      await wrapper.find('textarea').setValue('Please do not lose this.')
      await wrapper.find('form.composer').trigger('submit')
      await flushPromises()

      expect((wrapper.find('textarea').element as HTMLTextAreaElement).value).toBe('Please do not lose this.')
    })

    it('hides the composer and shows a create-new-ticket CTA for a closed ticket', async () => {
      fakeStore = makeFakeStore({ currentTicket: makeTicketDetails({ status: 'Closed' }) })
      const wrapper = await mountView()

      expect(wrapper.find('textarea').exists()).toBe(false)
      expect(wrapper.text()).toContain('This ticket is closed.')
      expect(wrapper.text()).toContain('Create New Ticket')
    })
  })
})
