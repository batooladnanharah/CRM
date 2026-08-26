<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = defineProps<{
  page: number
  pageSize: number
  totalCount: number
}>()

const emit = defineEmits<{ 'update:page': [page: number] }>()

const { t } = useI18n()

const totalPages = computed(() => Math.max(1, Math.ceil(props.totalCount / props.pageSize)))
const hasPrevious = computed(() => props.page > 1)
const hasNext = computed(() => props.page < totalPages.value)

function goPrevious() {
  if (hasPrevious.value) emit('update:page', props.page - 1)
}

function goNext() {
  if (hasNext.value) emit('update:page', props.page + 1)
}
</script>

<template>
  <nav class="pagination ui-pagination" :aria-label="t('common.pageOf', { page, total: totalPages })">
    <span>{{ t('common.pageOf', { page, total: totalPages }) }}</span>
    <button type="button" :disabled="!hasPrevious" @click="goPrevious">{{ t('common.previous') }}</button>
    <button type="button" :disabled="!hasNext" @click="goNext">{{ t('common.next') }}</button>
  </nav>
</template>

<style scoped>
.ui-pagination button:disabled {
  color: var(--color-text-muted);
  background: var(--color-surface);
  cursor: not-allowed;
}
</style>
