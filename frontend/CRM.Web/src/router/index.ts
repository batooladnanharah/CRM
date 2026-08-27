import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

export function createAppRouter() {
  const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
      {
        path: '/login',
        name: 'login',
        component: () => import('@/modules/auth/views/LoginView.vue'),
        meta: { public: true },
      },
      {
        path: '/',
        name: 'dashboard',
        component: () => import('@/modules/dashboard/views/DashboardView.vue'),
        meta: { requiresAuth: true, title: 'Dashboard' },
      },
      {
        path: '/forbidden',
        name: 'forbidden',
        component: () => import('@/modules/dashboard/views/ForbiddenView.vue'),
        meta: { requiresAuth: true, title: 'Customers' },
      },
      {
        path: '/customers',
        name: 'customers',
        component: () => import('@/modules/customers/views/CustomersListView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: '/customers/new',
        name: 'customer-create',
        component: () => import('@/modules/customers/views/CustomerCreateView.vue'),
        meta: { requiresAuth: true, permission: 'customers.manage', title: 'Create customer' },
      },
      {
        path: '/customers/:id',
        name: 'customer-profile',
        component: () => import('@/modules/customers/views/CustomerProfileView.vue'),
        meta: { requiresAuth: true, title: 'Customer profile' },
      },
      {
        path: '/customers/:id/edit',
        name: 'customer-edit',
        component: () => import('@/modules/customers/views/CustomerEditView.vue'),
        meta: { requiresAuth: true, permission: 'customers.manage', title: 'Edit customer' },
      },
      {
        path: '/tickets',
        name: 'tickets-list',
        component: () => import('@/modules/tickets/views/TicketsListView.vue'),
        meta: { requiresAuth: true, title: 'Tickets' },
      },
      {
        path: '/tickets/new',
        name: 'ticket-create',
        component: () => import('@/modules/tickets/views/TicketCreateView.vue'),
        meta: { requiresAuth: true, title: 'Create ticket' },
      },
      {
        path: '/tickets/:id',
        name: 'ticket-details',
        component: () => import('@/modules/tickets/views/TicketDetailsView.vue'),
        meta: { requiresAuth: true, title: 'Ticket details' },
      },
      {
        path: '/settings/quick-replies',
        name: 'quick-replies-management',
        component: () => import('@/modules/quickReplies/views/QuickRepliesManagementView.vue'),
        meta: { requiresAuth: true, permission: 'quickReplies.manage', title: 'Quick replies' },
      },
      {
        path: '/settings/sla',
        name: 'sla-policies-management',
        component: () => import('@/modules/sla/views/SlaPoliciesManagementView.vue'),
        meta: { requiresAuth: true, permission: 'sla.manage', title: 'SLA policies' },
      },
      {
        path: '/sla/escalation-rules',
        name: 'escalation-rules-management',
        component: () => import('@/modules/sla/views/EscalationRulesManagementView.vue'),
        meta: { requiresAuth: true, permission: 'sla.escalation.manage', title: 'Escalation rules' },
      },
      {
        path: '/knowledge-base',
        name: 'knowledge-base-management',
        component: () => import('@/modules/knowledgeBase/views/KnowledgeBaseManagementView.vue'),
        meta: { requiresAuth: true, title: 'Knowledge base' },
      },
      {
        path: '/knowledge-base/:id',
        name: 'knowledge-base-edit',
        component: () => import('@/modules/knowledgeBase/views/KnowledgeBaseManagementView.vue'),
        meta: { requiresAuth: true, title: 'Knowledge base' },
      },
      {
        path: '/admin/users',
        name: 'admin-users',
        component: () => import('@/modules/security/views/UsersAdminView.vue'),
        meta: { requiresAuth: true, permission: 'security.admin', title: 'Users' },
      },
      {
        path: '/admin/audit-log',
        name: 'admin-audit-log',
        component: () => import('@/modules/security/views/AuditLogView.vue'),
        meta: { requiresAuth: true, permission: 'security.admin', title: 'Audit log' },
      },
      {
        path: '/reports',
        name: 'reports',
        component: () => import('@/modules/reports/views/ReportsView.vue'),
        meta: { requiresAuth: true, permission: 'reports.view', title: 'Reports' },
      },
      {
        path: '/communication-channels',
        name: 'communication-channels-management',
        component: () =>
          import('@/modules/communicationChannels/views/CommunicationChannelsManagementView.vue'),
        meta: { requiresAuth: true, permission: 'channels.manage', title: 'Communication channels' },
      },
      {
        path: '/portal/dashboard',
        name: 'portal-dashboard',
        component: () => import('@/modules/customerPortal/views/PortalDashboardView.vue'),
        meta: { requiresAuth: true, portalOnly: true, title: 'My Dashboard' },
      },
      {
        path: '/portal/tickets',
        name: 'portal-tickets-list',
        component: () => import('@/modules/customerPortal/views/PortalTicketsListView.vue'),
        meta: { requiresAuth: true, portalOnly: true, title: 'My Tickets' },
      },
      {
        path: '/portal/tickets/new',
        name: 'portal-ticket-create',
        component: () => import('@/modules/customerPortal/views/PortalTicketCreateView.vue'),
        meta: { requiresAuth: true, portalOnly: true, title: 'Submit a Ticket' },
      },
      {
        path: '/portal/tickets/:id',
        name: 'portal-ticket-details',
        component: () => import('@/modules/customerPortal/views/PortalTicketDetailsView.vue'),
        meta: { requiresAuth: true, portalOnly: true, title: 'Ticket Details' },
      },
      { path: '/:pathMatch(.*)*', redirect: '/' },
    ],
  })

  router.beforeEach((to) => {
    const authStore = useAuthStore()

    if (to.meta.requiresAuth && !authStore.isAuthenticated) {
      return { path: '/login', query: { redirect: to.fullPath } }
    }

    if (to.meta.public && authStore.isAuthenticated) {
      return { path: '/' }
    }

    // The Customer role has its own portal surface — keep it off every
    // internal route, and keep every other role off the portal surface, via
    // redirect (not /forbidden) so each role always lands somewhere useful.
    if (authStore.isAuthenticated) {
      if (authStore.isCustomer && !to.meta.portalOnly && !to.meta.public) {
        return { name: 'portal-dashboard' }
      }
      if (!authStore.isCustomer && to.meta.portalOnly) {
        return { name: 'dashboard' }
      }
    }

    const permission = to.meta.permission as string | undefined
    if (permission && !authStore.can(permission)) {
      return { path: '/forbidden' }
    }

    return true
  })

  return router
}

const router = createAppRouter()

export default router
