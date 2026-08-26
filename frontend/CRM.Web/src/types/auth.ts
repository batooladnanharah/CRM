export interface AuthUser {
  id: string
  name: string
  email: string
  roles: string[]
}

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

