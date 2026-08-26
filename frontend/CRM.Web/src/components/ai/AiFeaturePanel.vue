<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppButton from '@/components/ui/AppButton.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import type { AiFeatureState } from '@/types/ai'

withDefaults(
  defineProps<{
    state: AiFeatureState
    generateLabel?: string
    provider?: string | null
  }>(),
  {
    generateLabel: undefined,
    provider: null,
  },
)

defineEmits<{ generate: []; cancel: []; retry: [] }>()

const { t } = useI18n()
</script>

<template>
  <div class="ai-feature-panel">
    <AppAlert v-if="state === 'unavailable'" tone="warning">
      {{ t('ai.feature.unavailable') }}
    </AppAlert>

    <AppButton v-else-if="state === 'idle'" type="button" @click="$emit('generate')">
      {{ generateLabel ?? t('ai.feature.generate') }}
    </AppButton>

    <div v-else-if="state === 'loading'" class="ai-feature-panel__loading">
      <LoadingState :label="t('ai.feature.loading')" />
      <AppButton type="button" variant="secondary" @click="$emit('cancel')">
        {{ t('ai.feature.cancel') }}
      </AppButton>
    </div>

    <div v-else-if="state === 'error'" class="ai-feature-panel__error">
      <ErrorState :message="t('ai.feature.error')" :retryable="false" />
      <AppButton type="button" @click="$emit('retry')">
        {{ t('ai.feature.retry') }}
      </AppButton>
    </div>

    <div v-else-if="state === 'success'" class="ai-feature-panel__success">
      <slot />
      <p v-if="provider === 'Development'" class="ai-feature-panel__disclaimer text-caption">
        {{ t('ai.feature.developmentDisclaimer') }}
      </p>
    </div>
  </div>
</template>

<style scoped>
.ai-feature-panel__loading,
.ai-feature-panel__error {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.ai-feature-panel__disclaimer {
  margin-top: var(--space-2);
}
</style>
