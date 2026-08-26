import { ApiError, NetworkError, authHeaders, readErrorMessage, resolveApiUrl } from './http'
import type { CustomerAttachment } from '@/types/customers'

export function listCustomerAttachments(customerId: string): Promise<CustomerAttachment[]> {
  return jsonRequest<CustomerAttachment[]>(`/customers/${customerId}/attachments`, {
    method: 'GET',
    headers: authHeaders(),
  })
}

export function uploadCustomerAttachment(
  customerId: string,
  file: File,
): Promise<CustomerAttachment> {
  const formData = new FormData()
  formData.append('file', file)

  // Do not set Content-Type manually — the browser sets the multipart boundary.
  return jsonRequest<CustomerAttachment>(`/customers/${customerId}/attachments`, {
    method: 'POST',
    headers: authHeaders(),
    body: formData,
  })
}

export async function downloadCustomerAttachment(
  customerId: string,
  attachmentId: string,
): Promise<Blob> {
  let response: Response
  try {
    response = await fetch(resolveApiUrl(`/customers/${customerId}/attachments/${attachmentId}/download`), {
      method: 'GET',
      headers: authHeaders(),
    })
  } catch {
    throw new NetworkError()
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readErrorMessage(response))
  }

  return response.blob()
}

export function deleteCustomerAttachment(customerId: string, attachmentId: string): Promise<void> {
  return jsonRequest<void>(`/customers/${customerId}/attachments/${attachmentId}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
}

async function jsonRequest<T>(path: string, init: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(resolveApiUrl(path), init)
  } catch {
    throw new NetworkError()
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readErrorMessage(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
