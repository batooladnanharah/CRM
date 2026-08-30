<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSlaPoliciesStore } from '@/stores/sla'
import { confirm } from '@/composables/useConfirm'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { SlaPolicy, TicketPriority } from '@/types/tickets'

const PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

const { t } = useI18n()
const store = useSlaPoliciesStore()

const isAdding = ref(false)
const editingId = ref<string | null>(null)
const draftName = ref('')
const draftChannel = ref('')
const draftPriority = ref<TicketPriority>('Normal')
const draftFirstResponseMinutes = ref(60)
const draftResolutionMinutes = ref(480)
const draftIsDefault = ref(false)
const draftIsActive = ref(true)

onMounted(() => {
  void store.fetch()
})

function resetDraft() {
  draftName.value = ''
  draftChannel.value = ''
  draftPriority.value = 'Normal'
  draftFirstResponseMinutes.value = 60
  draftResolutionMinutes.value = 480
  draftIsDefault.value = false
  draftIsActive.value = true
}

function openAddForm() {
  isAdding.value = true
  editingId.value = null
  resetDraft()
}

function startEdit(policy: SlaPolicy) {
  isAdding.value = false
  editingId.value = policy.id
  draftName.value = policy.name
  draftChannel.value = policy.channel ?? ''
  draftPriority.value = policy.priority
  draftFirstResponseMinutes.value = policy.firstResponseMinutes
  draftResolutionMinutes.value = policy.resolutionMinutes
  draftIsDefault.value = policy.isDefault
  draftIsActive.value = policy.isActive
}

function cancelForm() {
  isAdding.value = false
  editingId.value = null
}

function isDraftValid() {
  return (
    draftName.value.trim().length > 0 &&
    draftFirstResponseMinutes.value > 0 &&
    draftResolutionMinutes.value > 0
  )
}

function buildPayload() {
  return {
    name: draftName.value.trim(),
    channel: draftChannel.value.trim() || null,
    priority: draftPriority.value,
    firstResponseMinutes: draftFirstResponseMinutes.value,
    resolutionMinutes: draftResolutionMinutes.value,
    isDefault: draftIsDefault.value,
    isActive: draftIsActive.value,
  }
}

async function submitAdd() {
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

async function onToggleActive(policy: SlaPolicy) {
  try {
    await store.toggleActive(policy.id)
  } catch {
    // error surfaced via store.error
  }
}

async function onSetDefault(policy: SlaPolicy) {
  try {
    await store.setDefault(policy.id)
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(policy: SlaPolicy) {
  if (!(await confirm({ message: t('sla.policies.deleteConfirm'), tone: 'danger', confirmLabel: t('common.delete') }))) {
    return
  }
  try {
    await store.remove(policy.id)
  } catch {
    // error surfaced via store.error
  }
}
</script>

<template>
  <div class="sla-policies-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('sla.policies.title') }}</h1>
      </div>
      <AppButton type="button" @click="openAddForm" :disabled="isAdding">
        {{ t('sla.policies.new') }}
      </AppButton>
    </div>

    <AppAlert v-if="store.error" tone="danger" role="alert">{{ t(`sla.policies.errors.${store.error}`) }}</AppAlert>

    <form v-if="isAdding" class="surface sla-policy-form" @submit.prevent="submitAdd">
      <div class="field">
        <label for="sla-policy-name">{{ t('sla.policies.fields.name') }}</label>
        <input
          id="sla-policy-name"
          v-model="draftName"
          type="text"
          maxlength="200"
          :placeholder="t('sla.policies.fields.namePlaceholder')"
        />
      </div>
      <div class="field">
        <label for="sla-policy-channel">{{ t('sla.policies.fields.channel') }}</label>
        <input
          id="sla-policy-channel"
          v-model="draftChannel"
          type="text"
          maxlength="200"
          :placeholder="t('sla.policies.fields.channelAny')"
        />
      </div>
      <div class="field">
        <label for="sla-policy-priority">{{ t('sla.policies.fields.priority') }}</label>
        <select id="sla-policy-priority" v-model="draftPriority">
          <option v-for="priority in PRIORITIES" :key="priority" :value="priority">{{ priority }}</option>
        </select>
      </div>
      <div class="field">
        <label for="sla-policy-first-response">{{ t('sla.policies.fields.firstResponseMinutes') }}</label>
        <input
          id="sla-policy-first-response"
          v-model.number="draftFirstResponseMinutes"
          type="number"
          min="1"
        />
      </div>
      <div class="field">
        <label for="sla-policy-resolution">{{ t('sla.policies.fields.resolutionMinutes') }}</label>
        <input id="sla-policy-resolution" v-model.number="draftResolutionMinutes" type="number" min="1" />
      </div>
      <label class="active-toggle">
        <input type="checkbox" v-model="draftIsDefault" />
        {{ t('sla.policies.fields.isDefault') }}
      </label>
      <div class="form-actions">
        <AppButton type="submit" :loading="store.saving" :disabled="!isDraftValid()">
          {{ store.saving ? t('sla.policies.saving') : t('common.save') }}
        </AppButton>
        <AppButton variant="secondary" type="button" @click="cancelForm">{{ t('common.cancel') }}</AppButton>
      </div>
    </form>

    <LoadingState v-if="store.loading" :label="t('sla.policies.loading')" />
    <EmptyState v-else-if="store.items.length === 0 && !isAdding" :description="t('sla.policies.empty')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('sla.policies.fields.name') }}</th>
            <th>{{ t('sla.policies.fields.channel') }}</th>
            <th>{{ t('sla.policies.fields.priority') }}</th>
            <th>{{ t('sla.policies.fields.firstResponseMinutes') }}</th>
            <th>{{ t('sla.policies.fields.resolutionMinutes') }}</th>
            <th>{{ t('sla.policies.fields.isDefault') }}</th>
            <th>{{ t('sla.policies.fields.isActive') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="policy in store.items" :key="policy.id">
            <tr v-if="editingId === policy.id">
              <td colspan="8">
                <form class="sla-policy-inline-form" @submit.prevent="submitEdit">
                  <input
                    v-model="draftName"
                    type="text"
                    maxlength="200"
                    :placeholder="t('sla.policies.fields.namePlaceholder')"
                  />
                  <input
                    v-model="draftChannel"
                    type="text"
                    maxlength="200"
                    :placeholder="t('sla.policies.fields.channelAny')"
                  />
                  <select v-model="draftPriority">
                    <option v-for="priority in PRIORITIES" :key="priority" :value="priority">
                      {{ priority }}
                    </option>
                  </select>
                  <input v-model.number="draftFirstResponseMinutes" type="number" min="1" />
                  <input v-model.number="draftResolutionMinutes" type="number" min="1" />
                  <label class="active-toggle">
                    <input type="checkbox" v-model="draftIsDefault" />
                    {{ t('sla.policies.fields.isDefault') }}
                  </label>
                  <label class="active-toggle">
                    <input type="checkbox" v-model="draftIsActive" />
                    {{ t('sla.policies.fields.isActive') }}
                  </label>
                  <div class="form-actions">
                    <AppButton type="submit" :loading="store.saving">
                      {{ store.saving ? t('sla.policies.saving') : t('common.save') }}
                    </AppButton>
                    <AppButton variant="secondary" type="button" @click="cancelForm">{{ t('common.cancel') }}</AppButton>
                  </div>
                </form>
              </td>
            </tr>
            <tr v-else>
              <td>{{ policy.name }}</td>
              <td>{{ policy.channel ?? t('sla.policies.fields.channelAny') }}</td>
              <td>{{ policy.priority }}</td>
              <td>{{ policy.firstResponseMinutes }}</td>
              <td>{{ policy.resolutionMinutes }}</td>
              <td>{{ policy.isDefault ? '✓' : '' }}</td>
              <td>{{ policy.isActive ? '✓' : '' }}</td>
              <td>
                <AppButton variant="ghost" size="sm" type="button" @click="startEdit(policy)">{{ t('sla.policies.edit') }}</AppButton>
                <AppButton variant="ghost" size="sm" type="button" @click="onToggleActive(policy)">
                  {{ policy.isActive ? t('sla.policies.deactivate') : t('sla.policies.activate') }}
                </AppButton>
                <AppButton
                  v-if="!policy.isDefault"
                  variant="ghost"
                  size="sm"
                  type="button"
                  @click="onSetDefault(policy)"
                >
                  {{ t('sla.policies.setDefault') }}
                </AppButton>
                <AppButton variant="ghost" size="sm" type="button" @click="onDelete(policy)">{{ t('sla.policies.delete') }}</AppButton>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.sla-policies-view {
  max-width: 72rem;
  margin: var(--space-8) auto;
}

.sla-policy-form,
.sla-policy-inline-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-5);
  margin-bottom: var(--space-5);
}

.active-toggle {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.form-actions {
  display: flex;
  gap: var(--space-2);
}
</style>
