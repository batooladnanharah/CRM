<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(defineProps<{ message?: string; retryable?: boolean }>(), {
  retryable: true,
})

defineEmits<{ retry: [] }>()

const { t } = useI18n()
</script>

<template>
  <div class="ui-error-state error" role="alert">
    <p class="ui-error-state__message">{{ message ?? t('common.error') }}</p>
    <button v-if="retryable" type="button" class="ui-error-state__retry secondary-button" @click="$emit('retry')">
      {{ t('common.retry') }}
    </button>
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
