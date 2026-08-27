import { reactive, ref } from 'vue'
import { defineStore } from 'pinia'
import { ApiError } from '@/api/http'
import { getAiStatus, summariseTicket } from '@/api/ai'
import { i18n } from '@/i18n'
import { useToast } from '@/composables/useToast'
import type { AiStatus } from '@/types/ai'

const FALLBACK_STATUS: AiStatus = { enabled: false, provider: null, available: false }
const t = i18n.global.t

function errorCodeFrom(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 503) return 'unavailable'
    if (err.status === 502) return 'providerFailed'
    return 'generic'
  }
  return 'generic'
}

export const useAiStore = defineStore('ai', () => {
  const status = ref<AiStatus | null>(null)
  const loadingStatus = ref(false)
  const statusError = ref<string | null>(null)

  const summaries = reactive<Record<string, string>>({})
  const summaryLoading = reactive<Record<string, boolean>>({})
  const summaryError = reactive<Record<string, string | null>>({})

  async function loadStatus() {
    loadingStatus.value = true
    statusError.value = null

    try {
      status.value = await getAiStatus()
    } catch {
      statusError.value = 'errorLoad'
      status.value = { ...FALLBACK_STATUS }
    } finally {
      loadingStatus.value = false
    }
  }

  async function generateSummary(ticketId: string) {
    if (summaryLoading[ticketId]) {
      return
    }

    summaryLoading[ticketId] = true
    summaryError[ticketId] = null

    try {
      const response = await summariseTicket(ticketId)
      summaries[ticketId] = response.content ?? ''
      useToast().success(t('notifications.ai.completed'))
    } catch (err) {
      summaryError[ticketId] = errorCodeFrom(err)
      useToast().error(t('notifications.ai.failed'))
      throw err
    } finally {
      summaryLoading[ticketId] = false
    }
  }

  async function regenerateSummary(ticketId: string) {
    delete summaries[ticketId]
    await generateSummary(ticketId)
  }

  return {
    status,
    loadingStatus,
    statusError,
    summaries,
    summaryLoading,
    summaryError,
    loadStatus,
    generateSummary,
    regenerateSummary,
  }
})
