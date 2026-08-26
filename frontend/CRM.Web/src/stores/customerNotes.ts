import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createCustomerNote,
  deleteCustomerNote,
  listCustomerNotes,
  updateCustomerNote,
} from '@/api/customers'
import { ApiError } from '@/api/http'
import type { CustomerNote } from '@/types/customers'

function sortNewestFirst(notes: CustomerNote[]): CustomerNote[] {
  return [...notes].sort((a, b) => {
    const diff = new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime()
    return diff !== 0 ? diff : b.id.localeCompare(a.id)
  })
}

export const useCustomerNotesStore = defineStore('customerNotes', () => {
  const notes = ref<CustomerNote[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetchNotes(customerId: string) {
    loading.value = true
    error.value = null

    try {
      notes.value = sortNewestFirst(await listCustomerNotes(customerId))
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function addNote(customerId: string, content: string) {
    saving.value = true
    error.value = null

    try {
      const created = await createCustomerNote(customerId, { content })
      notes.value = sortNewestFirst([...notes.value, created])
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function editNote(customerId: string, noteId: string, content: string) {
    saving.value = true
    error.value = null

    try {
      const updated = await updateCustomerNote(customerId, noteId, { content })
      notes.value = sortNewestFirst(notes.value.map((n) => (n.id === noteId ? updated : n)))
      return updated
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        error.value = 'errorForbidden'
      } else {
        error.value = 'errorSave'
      }
      throw err
    } finally {
      saving.value = false
    }
  }

  async function removeNote(customerId: string, noteId: string) {
    saving.value = true
    error.value = null

    try {
      await deleteCustomerNote(customerId, noteId)
      notes.value = notes.value.filter((n) => n.id !== noteId)
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        error.value = 'errorForbidden'
      } else {
        error.value = 'errorDelete'
      }
      throw err
    } finally {
      saving.value = false
    }
  }

  return { notes, loading, saving, error, fetchNotes, addNote, editNote, removeNote }
})
