import { ApiError, NetworkError, authHeaders, readErrorMessage, resolveApiUrl } from './http'
import type { TicketAttachment } from '@/types/tickets'

export function listTicketAttachments(ticketId: string): Promise<TicketAttachment[]> {
  return jsonRequest<TicketAttachment[]>(`/tickets/${ticketId}/attachments`, {
    method: 'GET',
    headers: authHeaders(),
  })
}

export function uploadTicketAttachment(ticketId: string, file: File): Promise<TicketAttachment> {
  const formData = new FormData()
  formData.append('file', file)

  // Do not set Content-Type manually — the browser sets the multipart boundary.
  return jsonRequest<TicketAttachment>(`/tickets/${ticketId}/attachments`, {
    method: 'POST',
    headers: authHeaders(),
    body: formData,
  })
}

export async function downloadTicketAttachment(ticketId: string, attachmentId: string): Promise<Blob> {
  let response: Response
  try {
    response = await fetch(resolveApiUrl(`/tickets/${ticketId}/attachments/${attachmentId}/download`), {
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

export function deleteTicketAttachment(ticketId: string, attachmentId: string): Promise<void> {
  return jsonRequest<void>(`/tickets/${ticketId}/attachments/${attachmentId}`, {
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
