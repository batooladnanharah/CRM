<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Notification } from '@/types/notifications'

const props = defineProps<{ notification: Notification }>()
defineEmits<{ close: [id: string] }>()

const { t } = useI18n()

const role = computed(() => (props.notification.variant === 'error' ? 'alert' : 'status'))
const ariaLive = computed(() => (props.notification.variant === 'error' ? 'assertive' : 'polite'))
</script>

<template>
  <div
    class="app-toast"
    :class="`app-toast--${notification.variant}`"
    :role="role"
    :aria-live="ariaLive"
    aria-atomic="true"
  >
    <div class="app-toast__body">
      <strong v-if="notification.title" class="app-toast__title">{{ notification.title }}</strong>
      <p class="app-toast__message">{{ notification.message }}</p>
    </div>
    <button
      class="app-toast__dismiss"
      type="button"
      :aria-label="t('notifications.close')"
      @click="$emit('close', notification.id)"
    >×</button>
  </div>
</template>

<style scoped>
.app-toast {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
  min-width: 280px;
  max-width: 420px;
  padding: var(--space-3) var(--space-4);
  border-inline-start: 3px solid var(--color-status-info);
  border-radius: var(--radius-sm);
  background: var(--color-status-info-bg);
  color: var(--color-status-info);
  font-size: var(--font-size-sm);
  box-shadow: var(--shadow-md, 0 4px 12px rgba(0, 0, 0, 0.15));
}

.app-toast__body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.app-toast__title {
  font-weight: 600;
}

.app-toast__message {
  margin: 0;
}

.app-toast--success {
  border-inline-start-color: var(--color-status-success);
  background: var(--color-status-success-bg);
  color: var(--color-status-success);
}

.app-toast--warning {
  border-inline-start-color: var(--color-status-warning);
  background: var(--color-status-warning-bg);
  color: var(--color-status-warning);
}

.app-toast--error {
  border-inline-start-color: var(--color-status-danger);
  background: var(--color-status-danger-bg);
  color: var(--color-status-danger);
}

.app-toast__dismiss {
  padding: 0;
  min-height: auto;
  color: inherit;
  background: transparent;
  font-size: 1.1rem;
  line-height: 1;
}

.app-toast__dismiss:hover {
  background: transparent;
  opacity: 0.7;
}
</style>
