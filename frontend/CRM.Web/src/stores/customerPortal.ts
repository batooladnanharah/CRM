import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createPortalTicket,
  fetchPortalDashboard,
  fetchPortalTicket,
  fetchPortalTickets,
} from '@/api/customerPortal'
import type { CreateCustomerTicketPayload, CustomerDashboard, CustomerTicketDetails, CustomerTicketListItem } from '@/types/customerPortal'

export const useCustomerPortalStore = defineStore('customerPortal', () => {
  const dashboard = ref<CustomerDashboard | null>(null)
  const tickets = ref<CustomerTicketListItem[]>([])
  const currentTicket = ref<CustomerTicketDetails | null>(null)
  const loading = ref(false)
  const creating = ref(false)
  const error = ref<string | null>(null)

  async function fetchDashboard() {
    loading.value = true
    error.value = null

    try {
      dashboard.value = await fetchPortalDashboard()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function fetchTickets() {
    loading.value = true
    error.value = null

    try {
      tickets.value = await fetchPortalTickets()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function fetchTicket(id: string) {
    loading.value = true
    error.value = null
    currentTicket.value = null

    try {
      currentTicket.value = await fetchPortalTicket(id)
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function createTicket(payload: CreateCustomerTicketPayload) {
    creating.value = true
    error.value = null

    try {
      const created = await createPortalTicket(payload)
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      creating.value = false
    }
  }

  return {
    dashboard,
    tickets,
    currentTicket,
    loading,
    creating,
    error,
    fetchDashboard,
    fetchTickets,
    fetchTicket,
    createTicket,
  }
})
