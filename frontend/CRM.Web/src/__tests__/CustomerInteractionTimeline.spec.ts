import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import CustomerInteractionTimeline from '@/modules/customers/components/CustomerInteractionTimeline.vue'
import { i18n } from '@/i18n'
import type { getCustomerInteractions } from '@/api/customers'
import type { CustomerInteraction, PagedResult } from '@/types/customers'

const { getCustomerInteractionsMock } = vi.hoisted(() => ({
  getCustomerInteractionsMock: vi.fn<typeof getCustomerInteractions>(),
}))

vi.mock('@/api/customers', () => ({
  getCustomerInteractions: getCustomerInteractionsMock,
}))

function makeInteraction(overrides: Partial<CustomerInteraction> = {}): CustomerInteraction {
  return {
    id: '1',
    type: 'CustomerMessage',
    summary: 'Customer asked a question.',
    occurredAt: '2026-01-01T00:00:00Z',
    actorName: null,
    actorId: null,
    ticketId: null,
    ...overrides,
  }
}

function makePage(
  items: CustomerInteraction[],
  overrides: Partial<PagedResult<CustomerInteraction>> = {},
): PagedResult<CustomerInteraction> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, ...overrides }
}

beforeEach(() => {
  setActivePinia(createPinia())
  getCustomerInteractionsMock.mockReset()
})

function mountTimeline() {
  return mount(CustomerInteractionTimeline, {
    props: { customerId: 'customer-1' },
    global: { plugins: [i18n] },
  })
}

describe('CustomerInteractionTimeline', () => {
  it('shows a loading skeleton before the promise resolves', async () => {
    let resolveFetch!: (value: PagedResult<CustomerInteraction>) => void
    getCustomerInteractionsMock.mockReturnValue(
      new Promise((resolve) => {
        resolveFetch = resolve
      }),
    )

    const wrapper = mountTimeline()
    await nextTick()

    expect(wrapper.find('.skeleton').exists()).toBe(true)

    resolveFetch!(makePage([]))
    await flushPromises()
  })

  it('renders items in the order returned by the API', async () => {
    getCustomerInteractionsMock.mockResolvedValue(
      makePage([
        makeInteraction({ id: '1', summary: 'Newest' }),
        makeInteraction({ id: '2', summary: 'Oldest' }),
      ]),
    )

    const wrapper = mountTimeline()
    await flushPromises()

    const items = wrapper.findAll('.timeline > li')
    expect(items[0]!.text()).toContain('Newest')
    expect(items[1]!.text()).toContain('Oldest')
  })

  it('renders the empty state when there are no interactions', async () => {
    getCustomerInteractionsMock.mockResolvedValue(makePage([]))

    const wrapper = mountTimeline()
    await flushPromises()

    expect(wrapper.text()).toContain('No interactions yet.')
  })

  it('renders the error state and Retry re-invokes the fetch', async () => {
    getCustomerInteractionsMock.mockRejectedValue(new Error('network down'))

    const wrapper = mountTimeline()
    await flushPromises()

    expect(wrapper.text()).toContain('Unable to load interaction history.')

    getCustomerInteractionsMock.mockResolvedValue(makePage([makeInteraction()]))
    await wrapper.find('button').trigger('click')
    await flushPromises()

    expect(getCustomerInteractionsMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Customer asked a question.')
  })

  it('shows the ticket id when present and omits it when null', async () => {
    getCustomerInteractionsMock.mockResolvedValue(
      makePage([makeInteraction({ ticketId: 'ticket-123' })]),
    )

    const wrapper = mountTimeline()
    await flushPromises()

    expect(wrapper.text()).toContain('ticket-123')
  })

  it('hides pagination controls when totalCount fits within one page', async () => {
    getCustomerInteractionsMock.mockResolvedValue(makePage([makeInteraction()]))

    const wrapper = mountTimeline()
    await flushPromises()

    expect(wrapper.find('.pagination').exists()).toBe(false)
  })

  it('shows pagination controls and disables Previous on the first page', async () => {
    getCustomerInteractionsMock.mockResolvedValue(
      makePage([makeInteraction()], { pageSize: 1, totalCount: 3, page: 1 }),
    )

    const wrapper = mountTimeline()
    await flushPromises()

    const buttons = wrapper.findAll('.pagination button')
    const prevButton = buttons.find((b) => b.text() === 'Previous')!
    const nextButton = buttons.find((b) => b.text() === 'Next')!
    expect(prevButton.attributes('disabled')).toBeDefined()
    expect(nextButton.attributes('disabled')).toBeUndefined()
  })
})
