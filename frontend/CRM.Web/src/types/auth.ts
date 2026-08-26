export interface AuthUser {
  id: string
  name: string
  email: string
  roles: string[]
  // Optional so existing test fixtures/older cached sessionStorage payloads
  // built before RBAC (permissions: string[]) still satisfy the type; the
  // auth store treats a missing value as an empty permission set.
  permissions?: string[]
}

// Mirrors backend/CRM.Api/Auth/Permissions.cs — the single source of truth is
// still the backend catalogue; these constants exist so frontend call sites
// don't repeat permission strings by hand.
export const Permissions = {
  CustomersManage: 'customers.manage',
  TicketsManage: 'tickets.manage',
  TicketsEscalate: 'tickets.escalate',
  QuickRepliesView: 'quickReplies.view',
  QuickRepliesManage: 'quickReplies.manage',
  KnowledgeBaseView: 'kb.view',
  KnowledgeBaseManage: 'kb.manage',
  CommunicationChannelsView: 'channels.view',
  CommunicationChannelsManage: 'channels.manage',
  SlaManage: 'sla.manage',
  ReportsView: 'reports.view',
  SecurityAdmin: 'security.admin',
  PortalAccess: 'portal.access',
} as const

export interface LoginResponse {
  user: AuthUser
  token: string
}

// Canonical role name constants — mirrors backend/CRM.Api/Auth/Roles.cs.
// Roles are lowercase strings; a user may hold more than one.
export const Roles = {
  Admin: 'admin',
  Agent: 'agent',
  Customer: 'customer',
} as const

