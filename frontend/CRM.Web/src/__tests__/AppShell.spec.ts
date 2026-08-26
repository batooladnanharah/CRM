import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import AppShell from '@/components/AppShell.vue'
import { i18n } from '@/i18n'
import { useAuthStore } from '@/stores/auth'
import { Permissions } from '@/types/auth'

// Mirrors backend/CRM.Api/Auth/RolePermissions.cs for test fixtures — the
// frontend has no independent copy of this map at runtime (permissions
// always come from the login/[/me] response), so tests must supply it.
const ADMIN_PERMISSIONS = Object.values(Permissions)
const AGENT_PERMISSIONS = [
  Permissions.CustomersManage,
  Permissions.TicketsManage,
  Permissions.QuickRepliesView,
  Permissions.KnowledgeBaseView,
  Permissions.CommunicationChannelsView,
]
const CUSTOMER_PERMISSIONS = [Permissions.PortalAccess]

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/', name: 'dashboard', component: { template: '<div />' } },
      { path: '/customers', name: 'customers', component: { template: '<div />' } },
      { path: '/tickets', name: 'tickets-list', component: { template: '<div />' } },
      { path: '/knowledge-base', name: 'knowledge-base-management', component: { template: '<div />' } },
      { path: '/settings/quick-replies', name: 'quick-replies-management', component: { template: '<div />' } },
      {
        path: '/communication-channels',
        name: 'communication-channels-management',
        component: { template: '<div />' },
      },
      { path: '/settings/sla', name: 'sla-policies-management', component: { template: '<div />' } },
      { path: '/reports', name: 'reports', component: { template: '<div />' } },
      { path: '/admin/users', name: 'admin-users', component: { template: '<div />' } },
      { path: '/admin/audit-log', name: 'admin-audit-log', component: { template: '<div />' } },
      { path: '/portal/dashboard', name: 'portal-dashboard', component: { template: '<div />' } },
      { path: '/portal/tickets', name: 'portal-tickets-list', component: { template: '<div />' } },
      { path: '/portal/tickets/new', name: 'portal-ticket-create', component: { template: '<div />' } },
    ],
  })
}

async function mountShell(router: Router = makeRouter()) {
  router.push('/')
  await router.isReady()

  return mount(AppShell, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('AppShell navigation', () => {
  it('shows the Reports and Security links for an admin', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'], permissions: ADMIN_PERMISSIONS }

    const wrapper = await mountShell()

    expect(wrapper.text()).toContain('Reports')
    expect(wrapper.text()).toContain('Users')
    expect(wrapper.text()).toContain('Audit Log')
  })

  it('hides the Reports and Security links for an agent', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'], permissions: AGENT_PERMISSIONS }

    const wrapper = await mountShell()

    expect(wrapper.text()).not.toContain('Reports')
    expect(wrapper.text()).not.toContain('Audit Log')
  })

  it('hides the Reports link (and all internal nav) for a customer, showing the portal nav instead', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'], permissions: CUSTOMER_PERMISSIONS }

    const wrapper = await mountShell()

    expect(wrapper.text()).not.toContain('Reports')
    expect(wrapper.text()).toContain('My Tickets')
  })
})

describe('AppShell responsive behavior', () => {
  it('renders a menu-toggle button that opens the mobile drawer and a backdrop that closes it', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'], permissions: AGENT_PERMISSIONS }

    const wrapper = await mountShell()

    const menuToggle = wrapper.find('.menu-toggle')
    expect(menuToggle.exists()).toBe(true)

    expect(wrapper.find('.sidebar-backdrop').exists()).toBe(false)
    expect(wrapper.find('.sidebar').classes()).not.toContain('is-open')

    await menuToggle.trigger('click')

    expect(wrapper.find('.sidebar').classes()).toContain('is-open')
    expect(wrapper.find('.sidebar-backdrop').exists()).toBe(true)

    await wrapper.find('.sidebar-backdrop').trigger('click')

    expect(wrapper.find('.sidebar').classes()).not.toContain('is-open')
  })

  it('renders a tablet sidebar-toggle button that expands the collapsed rail', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'], permissions: AGENT_PERMISSIONS }

    const wrapper = await mountShell()

    const sidebarToggle = wrapper.find('.sidebar-toggle')
    expect(sidebarToggle.exists()).toBe(true)
    expect(wrapper.find('.sidebar').classes()).not.toContain('is-expanded')

    await sidebarToggle.trigger('click')

    expect(wrapper.find('.sidebar').classes()).toContain('is-expanded')
  })
})
