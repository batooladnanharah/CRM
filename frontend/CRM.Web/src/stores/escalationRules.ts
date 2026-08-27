import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  activateEscalationRule,
  createEscalationRule,
  deactivateEscalationRule,
  deleteEscalationRule,
  listEscalationRules,
  updateEscalationRule,
} from '@/api/escalationRules'
import type { CreateEscalationRulePayload, EscalationRule, UpdateEscalationRulePayload } from '@/types/notifications'

export const useEscalationRulesStore = defineStore('escalationRules', () => {
  const items = ref<EscalationRule[]>([])
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  async function fetch() {
    loading.value = true
    error.value = null

    try {
      items.value = await listEscalationRules()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function create(payload: CreateEscalationRulePayload) {
    saving.value = true
    error.value = null

    try {
      const created = await createEscalationRule(payload)
      items.value = [created, ...items.value]
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function update(id: string, payload: UpdateEscalationRulePayload) {
    saving.value = true
    error.value = null

    try {
      const updated = await updateEscalationRule(id, payload)
      items.value = items.value.map((r) => (r.id === id ? updated : r))
      return updated
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function activate(id: string) {
    saving.value = true
    error.value = null

    try {
      const updated = await activateEscalationRule(id)
      items.value = items.value.map((r) => (r.id === id ? updated : r))
      return updated
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      saving.value = false
    }
  }

  async function deactivate(id: string) {
    saving.value = true
    error.value = null

    try {
      const updated = await deactivateEscalationRule(id)
      items.value = items.value.map((r) => (r.id === id ? updated : r))
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
      await deleteEscalationRule(id)
      items.value = items.value.filter((r) => r.id !== id)
    } catch (err) {
      error.value = 'errorDelete'
      throw err
    } finally {
      saving.value = false
    }
  }

  return { items, loading, saving, error, fetch, create, update, activate, deactivate, remove }
})
