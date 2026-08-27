import { useNotificationStore } from '@/stores/notification'
import type { NotificationInput, NotificationVariant } from '@/types/notifications'

export function useToast() {
  const store = useNotificationStore()

  const push = (variant: NotificationVariant) => (input: string | NotificationInput) =>
    store.push(typeof input === 'string' ? { message: input, variant } : { ...input, variant })

  return {
    success: push('success'),
    error: push('error'),
    warning: push('warning'),
    info: push('info'),
    dismiss: store.dismiss,
    clear: store.clear,
  }
}
