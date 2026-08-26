<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTicketsStore } from '@/stores/tickets'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'

const props = defineProps<{ ticketId: string }>()

const { t, te } = useI18n()
const store = useTicketsStore()

const isOpen = ref(false)
const reason = ref('')

const errorText = computed(() => {
  if (!store.escalateError) {
    return null
  }
  const key = `tickets.errors.${store.escalateError}`
  return te(key) ? t(key) : store.escalateError
})

function open() {
  isOpen.value = true
  reason.value = ''
}

function cancel() {
  isOpen.value = false
  reason.value = ''
}

async function submit() {
  const trimmed = reason.value.trim()
  if (!trimmed) {
    return
  }
  try {
    await store.escalate(props.ticketId, trimmed)
    isOpen.value = false
    reason.value = ''
  } catch {
    // store.escalateError already carries the message to display.
  }
}
</script>

<template>
  <div class="escalate-ticket-dialog">
    <AppButton v-if="!isOpen" variant="secondary" size="sm" type="button" @click="open">{{ t('tickets.escalate.button') }}</AppButton>

    <form v-else class="escalate-form" @submit.prevent="submit">
      <label for="escalate-reason">{{ t('tickets.escalate.reasonLabel') }}</label>
      <textarea id="escalate-reason" v-model="reason" maxlength="500" rows="3" required></textarea>
      <div class="escalate-actions">
        <AppButton type="submit" size="sm" :loading="store.escalating" :disabled="!reason.trim()">
          {{ t('tickets.escalate.submit') }}
        </AppButton>
        <AppButton variant="ghost" size="sm" type="button" @click="cancel">{{ t('tickets.escalate.cancel') }}</AppButton>
      </div>
    </form>

    <AppAlert v-if="errorText" tone="danger" role="alert">{{ errorText }}</AppAlert>
  </div>
</template>

<style scoped>
.escalate-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-top: var(--space-2);
}

.escalate-actions {
  display: flex;
  gap: var(--space-2);
}
</style>
