<script setup lang="ts">
withDefaults(defineProps<{ title?: string }>(), {})
</script>

<template>
  <section class="ui-card surface">
    <header v-if="title || $slots.header" class="ui-card__header">
      <slot name="header">
        <h3 class="text-heading-3">{{ title }}</h3>
      </slot>
    </header>
    <div class="ui-card__body">
      <slot />
    </div>
    <footer v-if="$slots.footer" class="ui-card__footer">
      <slot name="footer" />
    </footer>
  </section>
</template>

<style scoped>
.ui-card {
  overflow: hidden;
}

.ui-card__header {
  padding: var(--space-5) var(--space-6);
  border-block-end: 1px solid var(--color-border);
}

.ui-card__body {
  padding: var(--space-6);
}

.ui-card__footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  padding: var(--space-4) var(--space-6);
  border-block-start: 1px solid var(--color-border);
}

@media (max-width: 640px) {
  .ui-card__header,
  .ui-card__body,
  .ui-card__footer {
    padding: var(--space-4);
  }
}
</style>
