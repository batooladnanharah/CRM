<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(defineProps<{ label?: string }>(), {})

const { t } = useI18n()
</script>

<template>
  <p class="ui-loading-state" role="status">
    <span class="ui-loading-state__spinner" aria-hidden="true"></span>
    {{ label ?? t('common.loading') }}
  </p>
</template>

<style scoped>
.ui-loading-state {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-6);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.ui-loading-state__spinner {
  width: 14px;
  height: 14px;
  border: 2px solid var(--color-border);
  border-inline-start-color: var(--teal);
  border-radius: 50%;
  animation: ui-loading-spin 0.7s linear infinite;
}

@keyframes ui-loading-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
