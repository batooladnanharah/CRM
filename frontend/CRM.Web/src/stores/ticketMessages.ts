import { ref } from 'vue'
import { defineStore } from 'pinia'
import { createTicketMessage, listTicketMessages } from '@/api/tickets'
import type { TicketMessage } from '@/types/tickets'

export const useTicketMessagesStore = defineStore('ticketMessages', () => {
  const items = ref<TicketMessage[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchMessages(ticketId: string) {
    loading.value = true
    error.value = null

    try {
      const result = await listTicketMessages(ticketId, 1, 100)
      items.value = result.items
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function addMessage(
    ticketId: string,
    body: string,
    isInternal: boolean,
    mentionedUserIds?: string[],
  ) {
    saving.value = true
    error.value = null

    try {
      const created = await createTicketMessage(ticketId, { body, isInternal, mentionedUserIds })
      items.value = [created, ...items.value]
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  return { items, loading, saving, error, fetchMessages, addMessage }
})
