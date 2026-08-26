import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getCustomerInteractions } from '@/api/customers'
import type { CustomerInteraction } from '@/types/customers'

export const useCustomerInteractionsStore = defineStore('customerInteractions', () => {
  const items = ref<CustomerInteraction[]>([])
  const page = ref(1)
  const pageSize = ref(20)
  const totalCount = ref(0)
  const loading = ref(false)
  const error = ref<string | null>(null)

  let lastCustomerId: string | null = null

  async function fetch(customerId: string, targetPage = 1) {
    lastCustomerId = customerId
    loading.value = true
    error.value = null

    try {
      const result = await getCustomerInteractions(customerId, targetPage, pageSize.value)
      items.value = result.items
      page.value = result.page
      pageSize.value = result.pageSize
      totalCount.value = result.totalCount
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  function retry() {
    if (lastCustomerId) {
      void fetch(lastCustomerId, page.value)
    }
  }

  function reset() {
    items.value = []
    page.value = 1
    pageSize.value = 20
    totalCount.value = 0
    loading.value = false
    error.value = null
    lastCustomerId = null
  }

  return { items, page, pageSize, totalCount, loading, error, fetch, retry, reset }
})
