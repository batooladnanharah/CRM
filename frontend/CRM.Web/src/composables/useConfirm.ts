import { reactive } from 'vue'

export interface ConfirmOptions {
  title?: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  tone?: 'neutral' | 'danger'
}

interface ConfirmState extends ConfirmOptions {
  open: boolean
  resolve: ((value: boolean) => void) | null
}

export const confirmState: ConfirmState = reactive({
  open: false,
  title: undefined,
  message: '',
  confirmLabel: undefined,
  cancelLabel: undefined,
  tone: 'neutral',
  resolve: null,
})

export function confirm(options: ConfirmOptions): Promise<boolean> {
  if (confirmState.resolve) {
    confirmState.resolve(false)
  }
  return new Promise((resolve) => {
    confirmState.title = options.title
    confirmState.message = options.message
    confirmState.confirmLabel = options.confirmLabel
    confirmState.cancelLabel = options.cancelLabel
    confirmState.tone = options.tone ?? 'neutral'
    confirmState.resolve = resolve
    confirmState.open = true
  })
}

export function resolveConfirm(value: boolean) {
  confirmState.resolve?.(value)
  confirmState.resolve = null
  confirmState.open = false
}
