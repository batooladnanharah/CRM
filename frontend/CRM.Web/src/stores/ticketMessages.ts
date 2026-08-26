import { ref } from 'vue'
import { defineStore } from 'pinia'
import { ApiError } from '@/api/http'
import { createTicketMessage, listTicketMessages } from '@/api/tickets'
import type { MessageChannel, TicketMessage } from '@/types/tickets'

export const useTicketMessagesStore = defineStore('ticketMessages', () => {
  const items = ref<TicketMessage[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)
  const sendError = ref<string | null>(null)

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
    channel: MessageChannel = 'Web',
    subjectOverride?: string,
  ) {
    saving.value = true
    error.value = null
    sendError.value = null

    try {
      const created = await createTicketMessage(ticketId, {
        body,
        isInternal,
        mentionedUserIds,
        channel,
        subjectOverride,
      })
      items.value = [created, ...items.value]
      return created
    } catch (err) {
      if (channel === 'Email') {
        sendError.value = err instanceof ApiError ? err.message : 'errorSave'
      } else {
        error.value = 'errorSave'
      }
      throw err
    } finally {
      saving.value = false
    }
  }

  return { items, loading, saving, error, sendError, fetchMessages, addMessage }
})
