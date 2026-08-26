import { apiRequest } from './http'
import type {
  Channel,
  CreateChannelPayload,
  EmailMessage,
  IngestEmailPayload,
  UpdateChannelPayload,
} from '@/types/communicationChannels'

export function listChannels(): Promise<Channel[]> {
  return apiRequest<Channel[]>('/channels', { method: 'GET' })
}

export function createChannel(payload: CreateChannelPayload): Promise<Channel> {
  return apiRequest<Channel>('/channels', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getChannel(id: string): Promise<Channel> {
  return apiRequest<Channel>(`/channels/${id}`, { method: 'GET' })
}

export function updateChannel(id: string, payload: UpdateChannelPayload): Promise<Channel> {
  return apiRequest<Channel>(`/channels/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteChannel(id: string): Promise<void> {
  return apiRequest<void>(`/channels/${id}`, { method: 'DELETE' })
}

export function listChannelEmails(channelId: string): Promise<EmailMessage[]> {
  return apiRequest<EmailMessage[]>(`/channels/${channelId}/emails`, { method: 'GET' })
}

export function ingestChannelEmail(
  channelId: string,
  payload: IngestEmailPayload,
): Promise<EmailMessage> {
  return apiRequest<EmailMessage>(`/channels/${channelId}/emails/ingest`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
