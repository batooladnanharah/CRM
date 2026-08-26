import { describe, it, expect, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { createAppRouter } from '@/router'
import { useAuthStore } from '@/stores/auth'

beforeEach(() => {
  setActivePinia(createPinia())
  sessionStorage.clear()
})

describe('router guards', () => {
  it('redirects an unauthenticated visit to / to /login with a redirect query', async () => {
    const router = createAppRouter()

    await router.push('/')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
    expect(router.currentRoute.value.query.redirect).toBe('/')
  })

  it('redirects an authenticated visit to /login to /', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'

    const router = createAppRouter()

    await router.push('/login')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/')
  })

  it('redirects an unauthenticated visit to /customers to /login', async () => {
    const router = createAppRouter()

    await router.push('/customers')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
    expect(router.currentRoute.value.query.redirect).toBe('/customers')
  })

  it('allows an authenticated visit to /customers', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'

    const router = createAppRouter()

    await router.push('/customers')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/customers')
  })

  it('redirects an unauthenticated visit to /customers/new to /login', async () => {
    const router = createAppRouter()

    await router.push('/customers/new')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects a customer-role visit to /customers/new to /portal/dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/customers/new')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/portal/dashboard')
  })

  it('allows an agent to visit /customers/new', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/customers/new')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/customers/new')
  })

  it('resolves /customers/:id to the customer-profile route', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'

    const router = createAppRouter()

    await router.push('/customers/123')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('customer-profile')
  })

  it('redirects an unauthenticated visit to /customers/:id to /login', async () => {
    const router = createAppRouter()

    await router.push('/customers/123')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects a customer-role visit to /customers/:id to /portal/dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/customers/123')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('portal-dashboard')
  })

  it('allows an agent to visit /customers/:id (profile is not role-restricted)', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/customers/123')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('customer-profile')
  })

  it('redirects an unauthenticated visit to /customers/:id/edit to /login', async () => {
    const router = createAppRouter()

    await router.push('/customers/123/edit')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects a customer-role visit to /customers/:id/edit to /portal/dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/customers/123/edit')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/portal/dashboard')
  })

  it('allows an admin to visit /customers/:id/edit', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/customers/123/edit')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/customers/123/edit')
  })
})

describe('router guards — tickets', () => {
  it('redirects an unauthenticated visit to /tickets to /login', async () => {
    const router = createAppRouter()

    await router.push('/tickets')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
    expect(router.currentRoute.value.query.redirect).toBe('/tickets')
  })

  it('allows an authenticated visit to /tickets', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'

    const router = createAppRouter()

    await router.push('/tickets')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/tickets')
  })

  it('redirects an unauthenticated visit to /tickets/new to /login', async () => {
    const router = createAppRouter()

    await router.push('/tickets/new')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('allows an authenticated visit to /tickets/new (no role restriction)', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/tickets/new')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/tickets/new')
  })

  it('redirects a customer-role visit to /tickets/new to /portal/dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/tickets/new')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/portal/dashboard')
  })

  it('resolves /tickets/:id to the ticket-details route', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'

    const router = createAppRouter()

    await router.push('/tickets/123')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('ticket-details')
  })

  it('redirects an unauthenticated visit to /tickets/:id to /login', async () => {
    const router = createAppRouter()

    await router.push('/tickets/123')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })
})

describe('router guards — quick replies management', () => {
  it('redirects an unauthenticated visit to /settings/quick-replies to /login', async () => {
    const router = createAppRouter()

    await router.push('/settings/quick-replies')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects an agent visit to /settings/quick-replies to /forbidden', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/settings/quick-replies')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows an admin to visit /settings/quick-replies', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/settings/quick-replies')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/settings/quick-replies')
  })
})

describe('router guards — communication channels', () => {
  it('redirects an unauthenticated visit to /communication-channels to /login', async () => {
    const router = createAppRouter()

    await router.push('/communication-channels')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects an agent visit to /communication-channels to /forbidden', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/communication-channels')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows an admin to visit /communication-channels and resolves to the management view', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/communication-channels')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/communication-channels')
    expect(router.currentRoute.value.name).toBe('communication-channels-management')
  })
})

describe('router guards — sla policies management', () => {
  it('redirects an unauthenticated visit to /settings/sla to /login', async () => {
    const router = createAppRouter()

    await router.push('/settings/sla')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects an agent visit to /settings/sla to /forbidden', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/settings/sla')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows an admin to visit /settings/sla', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/settings/sla')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/settings/sla')
    expect(router.currentRoute.value.name).toBe('sla-policies-management')
  })
})

describe('router guards — knowledge base', () => {
  it('redirects an unauthenticated visit to /knowledge-base to /login', async () => {
    const router = createAppRouter()

    await router.push('/knowledge-base')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects an unauthenticated visit to /knowledge-base/:id to /login', async () => {
    const router = createAppRouter()

    await router.push('/knowledge-base/some-id')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('allows an authenticated agent to visit /knowledge-base', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/knowledge-base')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/knowledge-base')
    expect(router.currentRoute.value.name).toBe('knowledge-base-management')
  })

  it('allows an authenticated agent to visit /knowledge-base/:id', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/knowledge-base/some-id')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/knowledge-base/some-id')
    expect(router.currentRoute.value.name).toBe('knowledge-base-edit')
  })
})

describe('router guards — role gating', () => {
  function addAdminOnlyRoute(router: ReturnType<typeof createAppRouter>) {
    router.addRoute({
      path: '/admin-only',
      name: 'admin-only',
      component: { template: '<div />' },
      meta: { requiresAuth: true, requiredRoles: ['admin'] },
    })
  }

  it('redirects to /forbidden when the user lacks a required role', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()
    addAdminOnlyRoute(router)

    await router.push('/admin-only')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows navigation when the user holds a required role (including as one of several roles)', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = {
      id: '1',
      name: 'Admin Agent',
      email: 'admin-agent@crm.local',
      roles: ['admin', 'agent'],
    }

    const router = createAppRouter()
    addAdminOnlyRoute(router)

    await router.push('/admin-only')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/admin-only')
  })

  it('redirects to /login (not /forbidden) when there is no token at all', async () => {
    const router = createAppRouter()
    addAdminOnlyRoute(router)

    await router.push('/admin-only')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })
})

describe('router guards — customer portal', () => {
  it('redirects an unauthenticated visit to /portal/dashboard to /login', async () => {
    const router = createAppRouter()

    await router.push('/portal/dashboard')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('allows a customer-role user to visit /portal/dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/portal/dashboard')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/portal/dashboard')
  })

  it('allows a customer-role user to visit /portal/tickets, /portal/tickets/new, and /portal/tickets/:id', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/portal/tickets')
    await router.isReady()
    expect(router.currentRoute.value.name).toBe('portal-tickets-list')

    await router.push('/portal/tickets/new')
    await router.isReady()
    expect(router.currentRoute.value.name).toBe('portal-ticket-create')

    await router.push('/portal/tickets/abc-123')
    await router.isReady()
    expect(router.currentRoute.value.name).toBe('portal-ticket-details')
  })

  it('redirects an agent visit to /portal/dashboard to /dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/portal/dashboard')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/')
    expect(router.currentRoute.value.name).toBe('dashboard')
  })

  it('redirects an admin visit to /portal/tickets to /dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/portal/tickets')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('dashboard')
  })

  it('redirects a customer-role visit to / (internal dashboard) to /portal/dashboard', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Portal Customer', email: 'customer@crm.local', roles: ['customer'] }

    const router = createAppRouter()

    await router.push('/')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('portal-dashboard')
  })
})

describe('router guards — reports', () => {
  it('redirects an unauthenticated visit to /reports to /login', async () => {
    const router = createAppRouter()

    await router.push('/reports')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects an agent visit to /reports to /forbidden', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/reports')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows an admin to visit /reports', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/reports')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/reports')
    expect(router.currentRoute.value.name).toBe('reports')
  })
})

describe('router guards — security administration', () => {
  it('redirects an unauthenticated visit to /admin/users to /login', async () => {
    const router = createAppRouter()

    await router.push('/admin/users')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('redirects an agent visit to /admin/users to /forbidden', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/admin/users')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows an admin to visit /admin/users', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/admin/users')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/admin/users')
    expect(router.currentRoute.value.name).toBe('admin-users')
  })

  it('redirects an agent visit to /admin/audit-log to /forbidden', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] }

    const router = createAppRouter()

    await router.push('/admin/audit-log')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/forbidden')
  })

  it('allows an admin to visit /admin/audit-log', async () => {
    const authStore = useAuthStore()
    authStore.token = 'a-valid-token'
    authStore.user = { id: '1', name: 'Admin', email: 'admin@crm.local', roles: ['admin'] }

    const router = createAppRouter()

    await router.push('/admin/audit-log')
    await router.isReady()

    expect(router.currentRoute.value.path).toBe('/admin/audit-log')
    expect(router.currentRoute.value.name).toBe('admin-audit-log')
  })
})
