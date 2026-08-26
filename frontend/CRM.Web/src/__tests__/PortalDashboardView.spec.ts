import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalDashboardView from '@/modules/customerPortal/views/PortalDashboardView.vue'
import { i18n } from '@/i18n'
import { useAuthStore } from '@/stores/auth'
import type { CustomerDashboard } from '@/types/customerPortal'

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    dashboard: null as CustomerDashboard | null,
    loading: false,
    error: null as string | null,
    fetchDashboard: vi.fn<() => Promise<void>>(),
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
      { path: '/portal/dashboard', name: 'portal-dashboard', component: PortalDashboardView },
      { path: '/portal/tickets', name: 'portal-tickets-list', component: { template: '<div />' } },
      { path: '/portal/tickets/new', name: 'portal-ticket-create', component: { template: '<div />' } },
      { path: '/portal/tickets/:id', name: 'portal-ticket-details', component: { template: '<div />' } },
    ],
  })
}

async function mountView(router: Router = makeRouter()) {
  router.push('/portal/dashboard')
  await router.isReady()

  return mount(PortalDashboardView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('PortalDashboardView', () => {
  it('calls fetchDashboard on mount', async () => {
    await mountView()

    expect(fakeStore.fetchDashboard).toHaveBeenCalledOnce()
  })

  it('shows the welcome banner with the customer name', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Jane Customer', email: 'jane@crm.local', roles: ['customer'] }

    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Jane Customer')
  })

  it('shows the loading skeleton while loading', async () => {
    fakeStore = makeFakeStore({ loading: true })
    const wrapper = await mountView()

    expect(wrapper.findAll('.skeleton').length).toBeGreaterThan(0)
  })

  it('shows metric counts once loaded', async () => {
    fakeStore = makeFakeStore({
      dashboard: { openCount: 2, pendingCount: 1, resolvedCount: 4, recentTickets: [] },
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('2')
    expect(wrapper.text()).toContain('1')
    expect(wrapper.text()).toContain('4')
  })

  it('shows the empty state when there are no recent tickets', async () => {
    fakeStore = makeFakeStore({
      dashboard: { openCount: 0, pendingCount: 0, resolvedCount: 0, recentTickets: [] },
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('You have no tickets yet.')
  })

  it('lists recent tickets when present', async () => {
    fakeStore = makeFakeStore({
      dashboard: {
        openCount: 1,
        pendingCount: 0,
        resolvedCount: 0,
        recentTickets: [
          {
            id: 'ticket-1',
            title: 'Cannot log in',
            status: 'Open',
            priority: 'Normal',
            createdAtUtc: '2026-01-01T00:00:00Z',
            updatedAtUtc: '2026-01-01T00:00:00Z',
          },
        ],
      },
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Cannot log in')
  })

  it('shows the error banner and retries on click', async () => {
    fakeStore = makeFakeStore({ error: 'errorLoad' })
    const wrapper = await mountView()

    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')

    expect(fakeStore.fetchDashboard).toHaveBeenCalledTimes(2)
  })
})
