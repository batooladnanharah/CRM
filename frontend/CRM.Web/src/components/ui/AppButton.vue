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
  min-height: 36px;
  padding-inline: var(--space-4);
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  color: white;
  background: var(--accent, var(--teal));
  font: 500 13px var(--font-sans, Arial, sans-serif);
  cursor: pointer;
  transition: background-color .15s ease, border-color .15s ease, color .15s ease;
}

.ui-button:hover:not(:disabled) {
  background: var(--accent-dark, var(--teal-dark));
}

.ui-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.ui-button--sm {
  min-height: 32px;
  padding-inline: var(--space-3);
  font-size: 12px;
}

.ui-button--secondary {
  color: var(--text-primary, var(--color-text-primary));
  background: transparent;
  border-color: var(--line, var(--color-border));
}

.ui-button--secondary:hover:not(:disabled) {
  background: var(--surface-2, #f7faf9);
  border-color: var(--accent, var(--teal));
}

.ui-button--ghost {
  color: var(--text-secondary, var(--color-text-secondary));
  background: transparent;
  border-color: transparent;
}

.ui-button--ghost:hover:not(:disabled) {
  color: var(--accent-dark, var(--teal-dark));
  background: var(--surface-2, #e7f4f0);
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
