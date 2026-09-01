<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import type { TicketPriority } from '@/types/tickets'

const TITLE_MAX_LENGTH = 200
const DESCRIPTION_MAX_LENGTH = 4000

const { t } = useI18n()
const router = useRouter()
const store = useCustomerPortalStore()

const PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

const title = ref('')
const description = ref('')
const priority = ref<TicketPriority>('Normal')
const titleError = ref<string | null>(null)
const descriptionError = ref<string | null>(null)

const descriptionId = 'portal-ticket-description'
const descriptionCharsRemaining = computed(() => DESCRIPTION_MAX_LENGTH - description.value.length)
const priorityHint = computed(() => t(`portal.ticket.submit.priorityHints.${priority.value}`))

function validate(): boolean {
  titleError.value = null
  descriptionError.value = null

  if (!title.value.trim()) {
    titleError.value = t('portal.ticket.submit.errors.titleRequired')
  } else if (title.value.trim().length > TITLE_MAX_LENGTH) {
    titleError.value = t('portal.ticket.submit.errors.titleTooLong')
  }

  if (!description.value.trim()) {
    descriptionError.value = t('portal.ticket.submit.errors.descriptionRequired')
  } else if (description.value.trim().length > DESCRIPTION_MAX_LENGTH) {
    descriptionError.value = t('portal.ticket.submit.errors.descriptionTooLong')
  }

  return !titleError.value && !descriptionError.value
}

async function onSubmit() {
  if (store.creating) {
    return
  }
  if (!validate()) {
    return
  }

  try {
    const ticket = await store.createTicket({
      title: title.value.trim(),
      description: description.value.trim(),
      priority: priority.value,
    })
    router.push({ name: 'portal-ticket-details', params: { id: ticket.id }, query: { submitted: '1' } })
  } catch {
    // store.error already carries the i18n key to display.
  }
}

function onCancel() {
  router.push({ name: 'portal-tickets-list' })
}
</script>

<template>
  <div class="portal-ticket-create-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('portal.dashboard.overline') }}</p>
        <h1>{{ t('portal.ticket.submit.title') }}</h1>
      </div>
    </div>

    <form class="surface form-surface" novalidate @submit.prevent="onSubmit">
      <div class="form-grid">
        <AppInput
          id="portal-ticket-title"
          v-model="title"
          :label="t('portal.ticket.submit.fields.subject')"
          type="text"
          maxlength="200"
          :placeholder="t('portal.ticket.submit.fields.subjectPlaceholder')"
          :error="titleError ?? undefined"
          :help="t('portal.ticket.submit.fields.charactersRemaining', { count: TITLE_MAX_LENGTH - title.length })"
          required
        />

        <div class="field">
          <label for="portal-ticket-priority">{{ t('portal.ticket.submit.fields.priority') }}</label>
          <select
            id="portal-ticket-priority"
            v-model="priority"
            class="priority-select"
            :class="`priority-select--${priority.toLowerCase()}`"
          >
            <option v-for="option in PRIORITIES" :key="option" :value="option">
              {{ t(`tickets.priorities.${option}`) }}
            </option>
          </select>
          <p class="ui-input__help">{{ priorityHint }}</p>
        </div>

        <div class="field field-wide">
          <label :for="descriptionId">
            {{ t('portal.ticket.submit.fields.description') }}
          </label>
          <textarea
            :id="descriptionId"
            v-model="description"
            class="ui-textarea"
            :class="{ 'ui-input--error': !!descriptionError }"
            maxlength="4000"
            rows="6"
            required
            :aria-invalid="!!descriptionError"
            :aria-describedby="`${descriptionId}-help`"
          ></textarea>
          <p v-if="descriptionError" :id="`${descriptionId}-help`" class="ui-input__error" role="alert">{{ descriptionError }}</p>
          <p v-else :id="`${descriptionId}-help`" class="ui-input__help">
            {{ t('portal.ticket.submit.fields.charactersRemaining', { count: descriptionCharsRemaining }) }}
          </p>
        </div>
      </div>

      <AppAlert v-if="store.error" tone="danger" role="alert" aria-live="polite">
        {{ t('portal.errors.generic') }}
      </AppAlert>

      <div class="form-actions">
        <AppButton variant="secondary" type="button" @click="onCancel">
          {{ t('portal.ticket.submit.cancel') }}
        </AppButton>
        <AppButton type="submit" :loading="store.creating">
          {{ t('portal.ticket.submit.submit') }}
        </AppButton>
      </div>
    </form>
  </div>
</template>

<style scoped>
.portal-ticket-create-view {
  max-width: 40rem;
  margin: var(--space-8) auto;
}

.ui-textarea {
  min-height: 160px;
  padding: var(--space-3);
  resize: vertical;
  color: var(--text-primary, var(--color-text-primary));
  background: white;
  border: 1px solid var(--line, var(--color-border));
  border-radius: var(--radius-md);
  font: 400 14px var(--font-sans, Arial, sans-serif);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.ui-textarea:focus-visible {
  outline: none;
  border-color: var(--accent, var(--teal));
  box-shadow: 0 0 0 3px rgba(24, 122, 108, 0.12);
}

.ui-textarea.ui-input--error {
  border-color: var(--color-status-danger);
}

.priority-select {
  border-inline-start: 3px solid var(--line);
  font-weight: 500;
  transition: border-color 0.15s ease, color 0.15s ease;
}

.priority-select--low {
  border-inline-start-color: var(--color-status-info);
  color: var(--color-status-info);
}

.priority-select--normal {
  border-inline-start-color: var(--color-status-success);
  color: var(--color-status-success);
}

.priority-select--high {
  border-inline-start-color: var(--color-status-warning);
  color: var(--color-status-warning);
}

.priority-select--urgent {
  border-inline-start-color: var(--color-status-danger);
  color: var(--color-status-danger);
}

.ui-input__error {
  margin: 0;
  color: var(--color-status-danger);
  font-size: var(--font-size-xs);
}

.ui-input__help {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
}
</style>
