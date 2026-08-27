export type NotificationVariant = 'success' | 'error' | 'warning' | 'info'

export interface NotificationInput {
  message: string
  variant?: NotificationVariant
  title?: string
  /** Milliseconds. 0 disables auto-dismiss. Undefined uses the variant default. */
  duration?: number
}

export interface Notification extends Required<Pick<NotificationInput, 'message'>> {
  id: string
  variant: NotificationVariant
  title?: string
  duration: number
  createdAt: number
}
