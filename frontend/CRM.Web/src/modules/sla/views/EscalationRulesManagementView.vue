<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useEscalationRulesStore } from '@/stores/escalationRules'
import { confirm } from '@/composables/useConfirm'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { EscalationRule, EscalationTrigger } from '@/types/notifications'

const TRIGGERS: EscalationTrigger[] = ['AtRisk', 'Breached']

const { t } = useI18n()
const store = useEscalationRulesStore()

const isAdding = ref(false)
const editingId = ref<string | null>(null)
const draftName = ref('')
const draftTrigger = ref<EscalationTrigger>('AtRisk')
const draftNotifyAgent = ref(true)
const draftNotifyManager = ref(false)
const draftIsActive = ref(true)
const showActionsError = ref(false)

onMounted(() => {
  void store.fetch()
})

function resetDraft() {
  draftName.value = ''
  draftTrigger.value = 'AtRisk'
  draftNotifyAgent.value = true
  draftNotifyManager.value = false
  draftIsActive.value = true
  showActionsError.value = false
}

function openAddForm() {
  isAdding.value = true
  editingId.value = null
  resetDraft()
}

function startEdit(rule: EscalationRule) {
  isAdding.value = false
  editingId.value = rule.id
  draftName.value = rule.name
  draftTrigger.value = rule.trigger
  draftNotifyAgent.value = rule.notifyAgent
  draftNotifyManager.value = rule.notifyManager
  draftIsActive.value = rule.isActive
  showActionsError.value = false
}

function cancelForm() {
  isAdding.value = false
  editingId.value = null
}

const hasAtLeastOneAction = computed(() => draftNotifyAgent.value || draftNotifyManager.value)

function isDraftValid() {
  return draftName.value.trim().length > 0 && draftName.value.trim().length <= 128 && hasAtLeastOneAction.value
}

function buildPayload() {
  return {
    name: draftName.value.trim(),
    trigger: draftTrigger.value,
    notifyAgent: draftNotifyAgent.value,
    notifyManager: draftNotifyManager.value,
    isActive: draftIsActive.value,
  }
}

async function submitAdd() {
  showActionsError.value = !hasAtLeastOneAction.value
  if (!isDraftValid()) {
    return
  }
  try {
    await store.create(buildPayload())
    isAdding.value = false
  } catch {
    // error surfaced via store.error
  }
}

async function submitEdit() {
  showActionsError.value = !hasAtLeastOneAction.value
  if (!editingId.value || !isDraftValid()) {
    return
  }
  try {
    await store.update(editingId.value, buildPayload())
    editingId.value = null
  } catch {
    // error surfaced via store.error
  }
}

async function onToggleActive(rule: EscalationRule) {
  try {
    if (rule.isActive) {
      await store.deactivate(rule.id)
    } else {
      await store.activate(rule.id)
    }
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(rule: EscalationRule) {
  if (!(await confirm({ message: t('sla.escalation.deleteConfirm'), tone: 'danger', confirmLabel: t('common.delete') }))) {
    return
  }
  try {
    await store.remove(rule.id)
  } catch {
    // error surfaced via store.error
  }
}

function actionsLabel(rule: EscalationRule) {
  if (rule.notifyAgent && rule.notifyManager) {
    return t('sla.escalation.actions.both')
  }
  if (rule.notifyAgent) {
    return t('sla.escalation.actions.notifyAgent')
  }
  if (rule.notifyManager) {
    return t('sla.escalation.actions.notifyManager')
  }
  return t('sla.escalation.actions.none')
}
</script>

<template>
  <div class="escalation-rules-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('sla.escalation.title') }}</h1>
      </div>
      <AppButton type="button" @click="openAddForm" :disabled="isAdding">
        {{ t('sla.escalation.create') }}
      </AppButton>
    </div>

    <AppAlert v-if="store.error" tone="danger" role="alert">{{ t(`sla.escalation.errors.${store.error}`) }}</AppAlert>

    <form v-if="isAdding" class="surface escalation-rule-form" @submit.prevent="submitAdd">
      <div class="field">
        <label for="escalation-rule-name">{{ t('sla.escalation.rule') }}</label>
        <input id="escalation-rule-name" v-model="draftName" type="text" maxlength="128" />
      </div>
      <div class="field">
        <label for="escalation-rule-trigger">{{ t('sla.escalation.trigger.label') }}</label>
        <select id="escalation-rule-trigger" v-model="draftTrigger">
          <option v-for="trigger in TRIGGERS" :key="trigger" :value="trigger">
            {{ t(`sla.escalation.trigger.${trigger === 'AtRisk' ? 'atRisk' : 'breached'}`) }}
          </option>
        </select>
      </div>
      <div class="toggle-group">
        <label class="active-toggle" :class="{ 'active-toggle--checked': draftNotifyAgent }">
          <input type="checkbox" v-model="draftNotifyAgent" />
          <span>{{ t('sla.escalation.actions.notifyAgent') }}</span>
        </label>
        <label class="active-toggle" :class="{ 'active-toggle--checked': draftNotifyManager }">
          <input type="checkbox" v-model="draftNotifyManager" />
          <span>{{ t('sla.escalation.actions.notifyManager') }}</span>
        </label>
        <label class="active-toggle" :class="{ 'active-toggle--checked': draftIsActive }">
          <input type="checkbox" v-model="draftIsActive" />
          <span>{{ t('sla.escalation.status.active') }}</span>
        </label>
      </div>
      <AppAlert v-if="showActionsError && !hasAtLeastOneAction" tone="danger" role="alert">
        {{ t('sla.escalation.validation.atLeastOneAction') }}
      </AppAlert>
      <div class="form-actions">
        <AppButton type="submit" :loading="store.saving">
          {{ store.saving ? t('sla.escalation.saving') : t('common.save') }}
        </AppButton>
        <AppButton variant="secondary" type="button" @click="cancelForm">{{ t('common.cancel') }}</AppButton>
      </div>
    </form>

    <LoadingState v-if="store.loading" :label="t('sla.escalation.loading')" />
    <EmptyState v-else-if="store.items.length === 0 && !isAdding" :description="t('sla.escalation.empty')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('sla.escalation.rule') }}</th>
            <th>{{ t('sla.escalation.trigger.label') }}</th>
            <th>{{ t('sla.escalation.actionsColumn') }}</th>
            <th>{{ t('sla.escalation.statusColumn') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="rule in store.items" :key="rule.id">
            <tr v-if="editingId === rule.id">
              <td colspan="5">
                <form class="escalation-rule-inline-form" @submit.prevent="submitEdit">
                  <input v-model="draftName" type="text" maxlength="128" />
                  <select v-model="draftTrigger">
                    <option v-for="trigger in TRIGGERS" :key="trigger" :value="trigger">
                      {{ t(`sla.escalation.trigger.${trigger === 'AtRisk' ? 'atRisk' : 'breached'}`) }}
                    </option>
                  </select>
                  <div class="toggle-group">
                    <label class="active-toggle" :class="{ 'active-toggle--checked': draftNotifyAgent }">
                      <input type="checkbox" v-model="draftNotifyAgent" />
                      <span>{{ t('sla.escalation.actions.notifyAgent') }}</span>
                    </label>
                    <label class="active-toggle" :class="{ 'active-toggle--checked': draftNotifyManager }">
                      <input type="checkbox" v-model="draftNotifyManager" />
                      <span>{{ t('sla.escalation.actions.notifyManager') }}</span>
                    </label>
                    <label class="active-toggle" :class="{ 'active-toggle--checked': draftIsActive }">
                      <input type="checkbox" v-model="draftIsActive" />
                      <span>{{ t('sla.escalation.status.active') }}</span>
                    </label>
                  </div>
                  <AppAlert v-if="showActionsError && !hasAtLeastOneAction" tone="danger" role="alert">
                    {{ t('sla.escalation.validation.atLeastOneAction') }}
                  </AppAlert>
                  <div class="form-actions">
                    <AppButton type="submit" :loading="store.saving">
                      {{ store.saving ? t('sla.escalation.saving') : t('common.save') }}
                    </AppButton>
                    <AppButton variant="secondary" type="button" @click="cancelForm">{{ t('common.cancel') }}</AppButton>
                  </div>
                </form>
              </td>
            </tr>
            <tr v-else>
              <td>{{ rule.name }}</td>
              <td>{{ t(`sla.escalation.trigger.${rule.trigger === 'AtRisk' ? 'atRisk' : 'breached'}`) }}</td>
              <td>{{ actionsLabel(rule) }}</td>
              <td>{{ rule.isActive ? t('sla.escalation.status.active') : t('sla.escalation.status.inactive') }}</td>
              <td>
                <AppButton variant="ghost" size="sm" type="button" @click="startEdit(rule)">{{ t('sla.escalation.edit') }}</AppButton>
                <AppButton variant="ghost" size="sm" type="button" @click="onToggleActive(rule)">
                  {{ rule.isActive ? t('sla.escalation.deactivate') : t('sla.escalation.activate') }}
                </AppButton>
                <AppButton variant="ghost" size="sm" type="button" @click="onDelete(rule)">{{ t('sla.escalation.delete') }}</AppButton>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.escalation-rules-view {
  max-width: 72rem;
  margin: var(--space-8) auto;
}

.escalation-rule-form,
.escalation-rule-inline-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-5);
  margin-bottom: var(--space-5);
}

.toggle-group {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.active-toggle {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  margin: 0;
  padding: var(--space-1) var(--space-3);
  border: 1px solid var(--line);
  border-radius: 999px;
  background: var(--surface);
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
  font-weight: 500;
  cursor: pointer;
  transition: background-color .15s ease, border-color .15s ease, color .15s ease;
}

.active-toggle:hover {
  border-color: var(--accent);
}

.active-toggle input[type='checkbox'] {
  width: 15px;
  height: 15px;
  margin: 0;
  accent-color: var(--accent);
  cursor: pointer;
}

.active-toggle--checked {
  background: var(--color-status-info-bg);
  border-color: var(--accent);
  color: var(--accent-dark);
  font-weight: 700;
}

.form-actions {
  display: flex;
  gap: var(--space-2);
}
</style>
