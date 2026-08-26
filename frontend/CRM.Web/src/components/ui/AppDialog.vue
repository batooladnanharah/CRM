<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

withDefaults(defineProps<{ title?: string }>(), {})

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()
const dialogRef = ref<HTMLElement | null>(null)
let previouslyFocused: HTMLElement | null = null

function focusableElements(): HTMLElement[] {
  if (!dialogRef.value) return []
  return Array.from(
    dialogRef.value.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])',
    ),
  )
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close')
    return
  }
  if (event.key !== 'Tab') return
  const elements = focusableElements()
  if (elements.length === 0) return
  const first = elements[0]!
  const last = elements[elements.length - 1]!
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first.focus()
  }
}

onMounted(async () => {
  previouslyFocused = document.activeElement as HTMLElement | null
  await nextTick()
  focusableElements()[0]?.focus()
  document.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown)
  previouslyFocused?.focus()
})
</script>

<template>
  <div class="ui-dialog-overlay" @click.self="$emit('close')">
    <div ref="dialogRef" class="ui-dialog surface" role="dialog" aria-modal="true" :aria-label="title">
      <header v-if="title || $slots.header" class="ui-dialog__header">
        <slot name="header">
          <h3 class="text-heading-3">{{ title }}</h3>
        </slot>
        <button class="ui-dialog__close icon-button" type="button" :aria-label="t('common.close')" @click="$emit('close')">×</button>
      </header>
      <div class="ui-dialog__body"><slot /></div>
      <footer v-if="$slots.footer" class="ui-dialog__footer">
        <slot name="footer" />
      </footer>
    </div>
  </div>
</template>

<style scoped>
.ui-dialog-overlay {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-4);
  background: rgba(24, 35, 45, 0.4);
  box-sizing: border-box;
}

.ui-dialog {
  width: 100%;
  max-width: 32rem;
  max-height: 90vh;
  overflow-y: auto;
}

.ui-dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-5) var(--space-6);
  border-block-end: 1px solid var(--color-border);
}

.ui-dialog__close {
  order: 1;
  margin-inline-start: auto;
}

.ui-dialog__body {
  padding: var(--space-6);
}

.ui-dialog__footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  padding: var(--space-4) var(--space-6);
  border-block-start: 1px solid var(--color-border);
}

@media (max-width: 640px) {
  .ui-dialog-overlay {
    padding: 0;
    align-items: stretch;
  }

  .ui-dialog {
    max-width: none;
    max-height: 100dvh;
    height: 100dvh;
    border-radius: 0;
  }
}
</style>
