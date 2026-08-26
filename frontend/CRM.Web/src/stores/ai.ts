import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getAiStatus, summariseTicket } from '@/api/ai'
import type { AiResponse, AiStatus } from '@/types/ai'

const FALLBACK_STATUS: AiStatus = { enabled: false, provider: null, available: false }

export const useAiStore = defineStore('ai', () => {
  const status = ref<AiStatus | null>(null)
  const loadingStatus = ref(false)
  const statusError = ref<string | null>(null)

  let summaryController: AbortController | null = null

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

  async function summarise(ticketId: string): Promise<AiResponse> {
    summaryController = new AbortController()
    try {
      return await summariseTicket(ticketId, summaryController.signal)
    } finally {
      summaryController = null
    }
  }

  function cancelSummary() {
    summaryController?.abort()
    summaryController = null
  }

  return { status, loadingStatus, statusError, loadStatus, summarise, cancelSummary }
})
