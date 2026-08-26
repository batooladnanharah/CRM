import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getReportsSummary } from '@/api/reports'
import type { ReportsSummary } from '@/types/reports'

export const useReportsStore = defineStore('reports', () => {
  const summary = ref<ReportsSummary | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const lastLoadedAt = ref<Date | null>(null)

  async function fetchSummary() {
    loading.value = true
    error.value = null

    try {
      summary.value = await getReportsSummary()
      lastLoadedAt.value = new Date()
    } catch {
      error.value = 'loadFailed'
    } finally {
      loading.value = false
    }
  }

  function reset() {
    summary.value = null
    loading.value = false
    error.value = null
    lastLoadedAt.value = null
  }

  return { summary, loading, error, lastLoadedAt, fetchSummary, reset }
})
