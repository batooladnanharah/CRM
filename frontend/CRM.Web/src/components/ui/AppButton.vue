<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
    size?: 'sm' | 'md'
    type?: 'button' | 'submit' | 'reset'
    loading?: boolean
    disabled?: boolean
  }>(),
  {
    variant: 'primary',
    size: 'md',
    type: 'button',
    loading: false,
    disabled: false,
  },
)
</script>

<template>
  <button
    class="ui-button"
    :class="[`ui-button--${variant}`, `ui-button--${size}`]"
    :type="type"
    :disabled="disabled || loading"
    :aria-busy="loading"
  >
    <span v-if="loading" class="ui-button__spinner" aria-hidden="true"></span>
    <slot />
  </button>
</template>

<style scoped>
.ui-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  min-height: var(--space-8);
  padding-inline: var(--space-4);
  border: 0;
  border-radius: var(--radius-sm);
  color: white;
  background: var(--teal);
  font: 700 var(--font-size-sm) Arial, sans-serif;
  cursor: pointer;
}

.ui-button:hover:not(:disabled) {
  background: var(--teal-dark);
}

.ui-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.ui-button--sm {
  min-height: 32px;
  padding-inline: var(--space-3);
  font-size: var(--font-size-xs);
}

.ui-button--secondary {
  color: var(--color-text-primary);
  background: #edf2f2;
}

.ui-button--secondary:hover:not(:disabled) {
  background: #dde7e7;
}

.ui-button--ghost {
  color: var(--teal-dark);
  background: transparent;
}

.ui-button--ghost:hover:not(:disabled) {
  background: #e7f4f0;
}

.ui-button--danger {
  background: var(--color-status-danger);
}

.ui-button--danger:hover:not(:disabled) {
  background: #841c17;
}

.ui-button__spinner {
  width: 13px;
  height: 13px;
  border: 2px solid rgba(255, 255, 255, 0.5);
  border-inline-start-color: white;
  border-radius: 50%;
  animation: ui-button-spin 0.7s linear infinite;
}

@keyframes ui-button-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 640px) {
  .ui-button {
    min-height: 44px;
  }
}
</style>
