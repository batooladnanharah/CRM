<script setup lang="ts">
import { useId } from 'vue'

withDefaults(
  defineProps<{
    label?: string
    modelValue?: string | number
    type?: string
    error?: string
    help?: string
    disabled?: boolean
    placeholder?: string
  }>(),
  {
    type: 'text',
  },
)

defineEmits<{ 'update:modelValue': [value: string] }>()

const inputId = useId()
</script>

<template>
  <div class="ui-input-field field">
    <label v-if="label" :for="inputId">{{ label }}</label>
    <input
      :id="inputId"
      class="ui-input"
      :class="{ 'ui-input--error': !!error }"
      :type="type"
      :value="modelValue"
      :disabled="disabled"
      :placeholder="placeholder"
      :aria-invalid="!!error"
      :aria-describedby="error ? `${inputId}-error` : help ? `${inputId}-help` : undefined"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <p v-if="error" :id="`${inputId}-error`" class="ui-input__error">{{ error }}</p>
    <p v-else-if="help" :id="`${inputId}-help`" class="ui-input__help">{{ help }}</p>
  </div>
</template>

<style scoped>
.ui-input {
  width: 100%;
  box-sizing: border-box;
  min-height: var(--space-8);
  padding: var(--space-3);
  color: var(--color-text-primary);
  background: white;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

.ui-input--error {
  border-color: var(--color-status-danger);
}

.ui-input__error {
  margin: 0;
  color: var(--color-status-danger);
  font-size: var(--font-size-xs);
}

.ui-input__help {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
}

@media (max-width: 640px) {
  .ui-input {
    min-height: 44px;
  }
}
</style>
