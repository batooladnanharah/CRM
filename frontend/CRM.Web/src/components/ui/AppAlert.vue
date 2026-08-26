<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(
  defineProps<{
    tone?: 'info' | 'success' | 'warning' | 'danger'
    dismissible?: boolean
  }>(),
  {
    tone: 'info',
    dismissible: false,
  },
)

defineEmits<{ dismiss: [] }>()

const { t } = useI18n()
</script>

<template>
  <div class="ui-alert" :class="`ui-alert--${tone}`" role="alert">
    <div class="ui-alert__body"><slot /></div>
    <button
      v-if="dismissible"
      class="ui-alert__dismiss"
      type="button"
      :aria-label="t('common.dismiss')"
      @click="$emit('dismiss')"
    >×</button>
  </div>
</template>

<style scoped>
.ui-alert {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  border-inline-start: 3px solid var(--color-status-info);
  border-radius: var(--radius-sm);
  background: var(--color-status-info-bg);
  color: var(--color-status-info);
  font-size: var(--font-size-sm);
}

.ui-alert__body {
  flex: 1;
}

.ui-alert--success {
  border-inline-start-color: var(--color-status-success);
  background: var(--color-status-success-bg);
  color: var(--color-status-success);
}

.ui-alert--warning {
  border-inline-start-color: var(--color-status-warning);
  background: var(--color-status-warning-bg);
  color: var(--color-status-warning);
}

.ui-alert--danger {
  border-inline-start-color: var(--color-status-danger);
  background: var(--color-status-danger-bg);
  color: var(--color-status-danger);
}

.ui-alert__dismiss {
  padding: 0;
  min-height: auto;
  color: inherit;
  background: transparent;
  font-size: 1.1rem;
  line-height: 1;
}

.ui-alert__dismiss:hover {
  background: transparent;
  opacity: 0.7;
}
</style>
