<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useNotificationStore } from '@/stores/notification'
import AppToast from '@/components/ui/AppToast.vue'

const { t } = useI18n()
const store = useNotificationStore()
</script>

<template>
  <div class="toast-host" role="region" :aria-label="t('notifications.regionLabel')" aria-live="polite">
    <TransitionGroup name="toast" tag="div" class="toast-host__stack">
      <AppToast
        v-for="notification in store.notifications"
        :key="notification.id"
        :notification="notification"
        @close="store.dismiss"
      />
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-host {
  position: fixed;
  top: var(--space-4, 16px);
  inset-inline-end: var(--space-4, 16px);
  z-index: 1000;
  pointer-events: none;
}

.toast-host__stack {
  display: flex;
  flex-direction: column;
  gap: var(--space-2, 8px);
}

.toast-host__stack > * {
  pointer-events: auto;
}

.toast-enter-active,
.toast-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
