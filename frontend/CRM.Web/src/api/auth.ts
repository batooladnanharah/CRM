import { apiRequest } from './http'
import type { AuthUser, LoginResponse } from '@/types/auth'

export function loginRequest(email: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export function meRequest(): Promise<AuthUser> {
  return apiRequest<AuthUser>('/auth/me', { method: 'GET' })
}

export function logoutRequest(): Promise<void> {
  return apiRequest<void>('/auth/logout', { method: 'POST' })
}
