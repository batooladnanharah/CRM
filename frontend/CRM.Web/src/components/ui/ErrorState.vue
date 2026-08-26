<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import AppButton from './AppButton.vue'

withDefaults(defineProps<{ message?: string; retryable?: boolean }>(), {
  retryable: true,
})

defineEmits<{ retry: [] }>()

const { t } = useI18n()
</script>

<template>
  <div class="ui-error-state error" role="alert">
    <p class="ui-error-state__message">{{ message ?? t('common.error') }}</p>
    <AppButton
      v-if="retryable"
      type="button"
      variant="secondary"
      size="sm"
      class="ui-error-state__retry"
      @click="$emit('retry')"
    >
      {{ t('common.retry') }}
    </AppButton>
  </div>
</template>

<style scoped>
.ui-error-state {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.ui-error-state__message {
  margin: 0;
}

.ui-error-state__retry {
  flex: 0 0 auto;
}
</style>
