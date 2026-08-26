<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTicketsStore } from '@/stores/tickets'

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
    <button v-if="!isOpen" type="button" @click="open">{{ t('tickets.escalate.button') }}</button>

    <form v-else class="escalate-form" @submit.prevent="submit">
      <label for="escalate-reason">{{ t('tickets.escalate.reasonLabel') }}</label>
      <textarea id="escalate-reason" v-model="reason" maxlength="500" rows="3" required></textarea>
      <div class="escalate-actions">
        <button type="submit" :disabled="store.escalating || !reason.trim()">
          {{ t('tickets.escalate.submit') }}
        </button>
        <button type="button" @click="cancel">{{ t('tickets.escalate.cancel') }}</button>
      </div>
    </form>

    <p v-if="errorText" role="alert">{{ errorText }}</p>
  </div>
</template>

<style scoped>
.escalate-form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.escalate-actions {
  display: flex;
  gap: 0.5rem;
}
</style>
