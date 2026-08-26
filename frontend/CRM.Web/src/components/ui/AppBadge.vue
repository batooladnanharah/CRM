<script setup lang="ts">
withDefaults(
  defineProps<{
    tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info'
  }>(),
  {
    tone: 'neutral',
  },
)
</script>

<template>
  <span class="ui-badge" :class="`ui-badge--${tone}`">
    <slot />
  </span>
</template>

<style scoped>
.ui-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.65rem;
  border-radius: 999px;
  color: var(--color-text-secondary);
  background: #eef2f2;
  font: 500 12px var(--font-sans, Arial, sans-serif);
  white-space: nowrap;
}

.ui-badge--success {
  color: var(--color-status-success);
  background: var(--color-status-success-bg);
}

.ui-badge--warning {
  color: var(--color-status-warning);
  background: var(--color-status-warning-bg);
}

.ui-badge--danger {
  color: var(--color-status-danger);
  background: var(--color-status-danger-bg);
}

.ui-badge--info {
  color: var(--color-status-info);
  background: var(--color-status-info-bg);
}
</style>
