<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAiStore } from '@/stores/ai'
import AppBadge from '@/components/ui/AppBadge.vue'

const { t } = useI18n()
const store = useAiStore()

const available = computed(() => store.status?.available ?? false)
</script>

<template>
  <AppBadge class="ai-availability-badge" :tone="available ? 'success' : 'neutral'">
    <span aria-hidden="true">{{ available ? '●' : '○' }}</span>
    {{ t('ai.status.label') }}
    {{ available ? t('ai.status.available') : t('ai.status.notConfigured') }}
  </AppBadge>
</template>
