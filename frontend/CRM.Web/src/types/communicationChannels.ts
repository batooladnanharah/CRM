export type ChannelType = 'Email'

export interface Channel {
  id: string
  name: string
  type: ChannelType
  isEnabled: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateChannelPayload {
  name: string
  type: ChannelType
}

export interface UpdateChannelPayload {
  name: string
  isEnabled: boolean
}

export interface EmailMessage {
  id: string
  channelId: string
  fromAddress: string
  toAddress: string
  subject: string
  body: string
  receivedAtUtc: string
  ticketId: string | null
}

export interface IngestEmailPayload {
  fromAddress: string
  toAddress: string
  subject: string
  body: string
  receivedAtUtc?: string
  ticketId?: string
}
