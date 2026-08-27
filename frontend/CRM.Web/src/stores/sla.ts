import { ref } from 'vue'
import { defineStore } from 'pinia'
import { createSlaPolicy, deleteSlaPolicy, listSlaPolicies, updateSlaPolicy } from '@/api/sla'
import { i18n } from '@/i18n'
import { useToast } from '@/composables/useToast'
import type { CreateSlaPolicyPayload, SlaPolicy, UpdateSlaPolicyPayload } from '@/types/tickets'

const t = i18n.global.t

export const useSlaPoliciesStore = defineStore('slaPolicies', () => {
  const items = ref<SlaPolicy[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetch() {
    loading.value = true
    error.value = null

    try {
      items.value = await listSlaPolicies()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateSlaPolicyPayload) {
    saving.value = true
    error.value = null

    try {
      const created = await createSlaPolicy(payload)
      items.value = [...items.value, created].sort((a, b) => a.name.localeCompare(b.name))
      useToast().success(t('notifications.sla.policySaved'))
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateSlaPolicyPayload) {
    saving.value = true
    error.value = null

    try {
      const updated = await updateSlaPolicy(id, payload)
      items.value = items.value.map((p) => (p.id === id ? updated : p))
      useToast().success(t('notifications.sla.policySaved'))
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
      await deleteSlaPolicy(id)
      items.value = items.value.filter((p) => p.id !== id)
    } catch (err) {
      error.value = 'errorDelete'
      throw err
    } finally {
      saving.value = false
    }
  }

  return { items, loading, saving, error, fetch, create, update, remove }
})
