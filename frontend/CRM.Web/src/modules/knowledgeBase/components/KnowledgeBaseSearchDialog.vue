<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { searchArticles } from '@/api/knowledgeBase'
import { ApiError } from '@/api/http'
import AppButton from '@/components/ui/AppButton.vue'
import type { KnowledgeBaseSearchItem } from '@/types/knowledgeBase'

// Dialog-scoped search state — intentionally NOT the shared
// useKnowledgeBaseStore().search state, so this dialog can be open at the
// same time as the management view (e.g. from a ticket) without either one
// clobbering the other's in-progress search.
const MIN_QUERY_LENGTH = 2

const emit = defineEmits<{ close: []; 'select-article': [id: string] }>()

const { t } = useI18n()
const router = useRouter()

const query = ref('')
const submittedQuery = ref('')

const state = reactive({
  items: [] as KnowledgeBaseSearchItem[],
  loading: false,
  error: null as string | null,
})

let requestId = 0

async function onSubmit() {
  const trimmed = query.value.trim()
  submittedQuery.value = trimmed

  if (trimmed.length < MIN_QUERY_LENGTH) {
    state.items = []
    state.error = null
    return
  }

  const current = ++requestId
  state.loading = true
  state.error = null

  try {
    const result = await searchArticles({ q: trimmed })
    if (current !== requestId) {
      return
    }
    state.items = result.items
  } catch (err) {
    if (current !== requestId) {
      return
    }
    state.error = err instanceof ApiError ? err.message : 'errorLoad'
    state.items = []
  } finally {
    if (current === requestId) {
      state.loading = false
    }
  }
}

function onSelect(item: KnowledgeBaseSearchItem) {
  emit('select-article', item.id)
  router.push({ name: 'knowledge-base-edit', params: { id: item.id } })
}
</script>

<template>
  <div class="kb-search-dialog surface" role="dialog" :aria-label="t('knowledgeBase.title')">
    <form class="kb-search-header" @submit.prevent="onSubmit">
      <input
        v-model="query"
        type="search"
        class="kb-search-input"
        maxlength="200"
        :placeholder="t('knowledgeBase.search.placeholder')"
        autofocus
      />
      <AppButton type="submit" size="sm">{{ t('knowledgeBase.search.submit') }}</AppButton>
      <AppButton
        type="button"
        variant="ghost"
        size="sm"
        :aria-label="t('common.close')"
        @click="emit('close')"
      >
        {{ t('common.close') }}
      </AppButton>
    </form>

    <p v-if="state.loading">{{ t('common.loading') }}</p>

    <p v-else-if="state.error" role="alert" class="kb-search-hint">
      {{ t('knowledgeBase.search.error') }}
    </p>

    <p
      v-else-if="submittedQuery.length > 0 && submittedQuery.length < MIN_QUERY_LENGTH"
      class="kb-search-hint"
    >
      {{ t('knowledgeBase.search.validation.tooShort') }}
    </p>

    <p v-else-if="submittedQuery.length >= MIN_QUERY_LENGTH && state.items.length === 0" class="kb-search-hint">
      {{ t('knowledgeBase.search.noResults') }}
    </p>

    <ul v-else-if="state.items.length > 0" class="kb-search-results">
      <li
        v-for="item in state.items"
        :key="item.id"
        class="kb-search-result"
        @click="onSelect(item)"
      >
        <p class="kb-search-result-title">{{ item.title }}</p>
        <p class="kb-search-result-snippet">{{ item.excerpt }}</p>
        <p class="kb-search-result-category">{{ item.category.name }}</p>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.kb-search-dialog {
  max-width: 32rem;
  padding: var(--space-4);
}

.kb-search-header {
  display: flex;
  gap: var(--space-2);
  margin-bottom: var(--space-3);
}

.kb-search-input {
  flex: 1;
}

.kb-search-hint {
  color: var(--muted);
}

.kb-search-results {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.kb-search-result {
  cursor: pointer;
  padding: var(--space-2);
  border-radius: var(--radius-sm);
}

.kb-search-result:hover {
  background: #f5fbf9;
}

.kb-search-result-title {
  font-weight: 700;
}

.kb-search-result-snippet {
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.kb-search-result-category {
  color: var(--muted);
  font-size: var(--font-size-xs);
}
</style>
