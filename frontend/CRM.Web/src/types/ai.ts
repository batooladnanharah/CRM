export type AiFeature =
  | 'TicketSummary'
  | 'TicketCategorization'
  | 'SuggestedReply'
  | 'SuggestedSolution'
  | 'Chatbot'

export interface AiStatus {
  enabled: boolean
  provider: string | null
  available: boolean
}

export interface AiResponse {
  success: boolean
  content: string | null
  provider: string
  model: string | null
  errorCode: string | null
}

export type AiFeatureState = 'idle' | 'loading' | 'success' | 'error' | 'unavailable'
