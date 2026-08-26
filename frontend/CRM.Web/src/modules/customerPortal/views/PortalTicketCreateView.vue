<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import type { TicketPriority } from '@/types/tickets'

const { t } = useI18n()
const router = useRouter()
const store = useCustomerPortalStore()

const PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

const title = ref('')
const description = ref('')
const priority = ref<TicketPriority>('Normal')
const fieldError = ref<string | null>(null)

function validate(): boolean {
  if (!title.value.trim()) {
    fieldError.value = t('portal.ticket.submit.errors.titleRequired')
    return false
  }
  if (title.value.trim().length > 200) {
    fieldError.value = t('portal.ticket.submit.errors.titleTooLong')
    return false
  }
  if (!description.value.trim()) {
    fieldError.value = t('portal.ticket.submit.errors.descriptionRequired')
    return false
  }
  if (description.value.trim().length > 4000) {
    fieldError.value = t('portal.ticket.submit.errors.descriptionTooLong')
    return false
  }

  fieldError.value = null
  return true
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
          required
        />

        <div class="field">
          <label for="portal-ticket-description">
            {{ t('portal.ticket.submit.fields.description') }}
          </label>
          <textarea
            id="portal-ticket-description"
            v-model="description"
            maxlength="4000"
            rows="6"
            required
          ></textarea>
        </div>

        <div class="field">
          <label for="portal-ticket-priority">{{ t('portal.ticket.submit.fields.priority') }}</label>
          <select id="portal-ticket-priority" v-model="priority">
            <option v-for="option in PRIORITIES" :key="option" :value="option">
              {{ t(`tickets.priorities.${option}`) }}
            </option>
          </select>
        </div>
      </div>

      <AppAlert v-if="fieldError" tone="danger" role="alert" aria-live="polite">{{ fieldError }}</AppAlert>
      <AppAlert v-else-if="store.error" tone="danger" role="alert" aria-live="polite">
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
  max-width: 30rem;
  margin: var(--space-8) auto;
}
</style>
