import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import DashboardView from '@/modules/dashboard/views/DashboardView.vue'
import { i18n } from '@/i18n'
import { useAuthStore } from '@/stores/auth'
import type { DashboardSummary, RecentCustomerEntry } from '@/types/dashboard'
import type { TicketListItem } from '@/types/tickets'

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    summary: null as DashboardSummary | null,
    myOpenTickets: [] as TicketListItem[],
    myTasks: [] as TicketListItem[],
    recentCustomers: [] as RecentCustomerEntry[],
    loading: false,
    error: null as string | null,
    loadAll: vi.fn<() => Promise<void>>(),
    refresh: vi.fn<() => Promise<void>>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/dashboard', () => ({
  useDashboardStore: () => fakeStore,
}))

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/', name: 'dashboard', component: DashboardView },
      { path: '/tickets', name: 'tickets-list', component: { template: '<div />' } },
      { path: '/tickets/:id', name: 'ticket-details', component: { template: '<div />' } },
      { path: '/customers/:id', name: 'customer-profile', component: { template: '<div />' } },
    ],
  })
}

async function mountDashboard(router: Router) {
  router.push('/')
  await router.isReady()

  return mount(DashboardView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('DashboardView', () => {
  it('renders all four widget titles', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const wrapper = await mountDashboard(makeRouter())

    expect(wrapper.text()).toContain('Open tickets assigned to me')
    expect(wrapper.text()).toContain('My open tickets')
    expect(wrapper.text()).toContain('Recent customers')
    expect(wrapper.text()).toContain('Tasks & follow-ups')
  })

  it('calls loadAll on mount', async () => {
    const wrapper = await mountDashboard(makeRouter())
    void wrapper

    expect(fakeStore.loadAll).toHaveBeenCalledOnce()
  })

  it('calls refresh when the Refresh button is clicked', async () => {
    const wrapper = await mountDashboard(makeRouter())

    const buttons = wrapper.findAll('button')
    const refreshButton = buttons.find((b) => b.text() === 'Refresh')!
    await refreshButton.trigger('click')

    expect(fakeStore.refresh).toHaveBeenCalledOnce()
  })

  it('shows the error banner when store.error is set', async () => {
    fakeStore = makeFakeStore({ error: 'loadFailed' })
    const wrapper = await mountDashboard(makeRouter())

    expect(wrapper.text()).toContain('Some widgets failed to load. Try again.')
  })

  it('does not show the error banner when there is no error', async () => {
    const wrapper = await mountDashboard(makeRouter())

    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
  })
})
