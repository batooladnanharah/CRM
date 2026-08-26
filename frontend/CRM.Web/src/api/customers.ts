import { apiRequest } from './http'
import type {
  CreateCustomerNotePayload,
  CreateCustomerPayload,
  Customer,
  CustomerInteraction,
  CustomerListQuery,
  CustomerNote,
  PagedResult,
  UpdateCustomerNotePayload,
  UpdateCustomerPayload,
} from '@/types/customers'

export function listCustomers(query: CustomerListQuery = {}): Promise<PagedResult<Customer>> {
  const params = new URLSearchParams()

  if (query.search?.trim()) params.set('search', query.search.trim())
  if (query.company?.trim()) params.set('company', query.company.trim())
  if (query.sortBy !== undefined) params.set('sortBy', query.sortBy)
  if (query.sortDir !== undefined) params.set('sortDir', query.sortDir)
  if (query.page !== undefined) params.set('page', String(query.page))
  if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize))

  const queryString = params.toString()
  const path = queryString ? `/customers?${queryString}` : '/customers'

  return apiRequest<PagedResult<Customer>>(path, { method: 'GET' })
}

export function createCustomer(payload: CreateCustomerPayload): Promise<Customer> {
  return apiRequest<Customer>('/customers', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getCustomer(id: string): Promise<Customer> {
  return apiRequest<Customer>(`/customers/${id}`, { method: 'GET' })
}

export function updateCustomer(id: string, payload: UpdateCustomerPayload): Promise<Customer> {
  return apiRequest<Customer>(`/customers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function getCustomerInteractions(
  customerId: string,
  page = 1,
  pageSize = 20,
): Promise<PagedResult<CustomerInteraction>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return apiRequest<PagedResult<CustomerInteraction>>(
    `/customers/${customerId}/interactions?${params.toString()}`,
    { method: 'GET' },
  )
}

export function listCustomerNotes(customerId: string): Promise<CustomerNote[]> {
  return apiRequest<CustomerNote[]>(`/customers/${customerId}/notes`, { method: 'GET' })
}

export function createCustomerNote(
  customerId: string,
  payload: CreateCustomerNotePayload,
): Promise<CustomerNote> {
  return apiRequest<CustomerNote>(`/customers/${customerId}/notes`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateCustomerNote(
  customerId: string,
  noteId: string,
  payload: UpdateCustomerNotePayload,
): Promise<CustomerNote> {
  return apiRequest<CustomerNote>(`/customers/${customerId}/notes/${noteId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteCustomerNote(customerId: string, noteId: string): Promise<void> {
  return apiRequest<void>(`/customers/${customerId}/notes/${noteId}`, { method: 'DELETE' })
}
