import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createQuickReply,
  deleteQuickReply,
  listQuickReplies,
  updateQuickReply,
} from '@/api/quickReplies'
import type { CreateQuickReplyPayload, QuickReply, UpdateQuickReplyPayload } from '@/types/tickets'

export const useQuickRepliesStore = defineStore('quickReplies', () => {
  const items = ref<QuickReply[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)
  const search = ref('')

  async function fetch(term?: string) {
    if (term !== undefined) {
      search.value = term
    }

    loading.value = true
    error.value = null

    try {
      items.value = await listQuickReplies(search.value || undefined)
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateQuickReplyPayload) {
    saving.value = true
    error.value = null

    try {
      const created = await createQuickReply(payload)
      items.value = [...items.value, created].sort((a, b) => a.title.localeCompare(b.title))
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateQuickReplyPayload) {
    saving.value = true
    error.value = null

    try {
      const updated = await updateQuickReply(id, payload)
      items.value = items.value.map((q) => (q.id === id ? updated : q))
      return updated
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function remove(id: string) {
    saving.value = true
    error.value = null

    try {
      await deleteQuickReply(id)
      items.value = items.value.filter((q) => q.id !== id)
    } catch (err) {
      error.value = 'errorDelete'
      throw err
    } finally {
      saving.value = false
    }
  }

  return { items, loading, saving, error, search, fetch, create, update, remove }
})
