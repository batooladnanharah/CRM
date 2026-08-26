<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(defineProps<{ title?: string; description?: string }>(), {})

const { t } = useI18n()
</script>

<template>
  <div class="ui-empty-state empty-state">
    <p class="text-heading-3">{{ title ?? t('common.emptyState.title') }}</p>
    <p class="text-body">
      <slot>{{ description ?? t('common.emptyState.description') }}</slot>
    </p>
  </div>
</template>

<style scoped>
.ui-empty-state {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  align-items: center;
}
</style>
