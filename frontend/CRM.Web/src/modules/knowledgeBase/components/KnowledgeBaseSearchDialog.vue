<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useKnowledgeBaseStore } from '@/stores/knowledgeBase'
import type { KnowledgeBaseArticle } from '@/types/knowledgeBase'

const SEARCH_DEBOUNCE_MS = 300
const RESULT_LIMIT = 10
const SNIPPET_LENGTH = 160

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()
const router = useRouter()
const store = useKnowledgeBaseStore()

const query = ref('')
let debounceHandle: ReturnType<typeof setTimeout> | null = null

function snippet(body: string): string {
  return body.length > SNIPPET_LENGTH ? `${body.slice(0, SNIPPET_LENGTH)}…` : body
}

function onInput() {
  if (debounceHandle) {
    clearTimeout(debounceHandle)
    debounceHandle = null
  }

  const term = query.value.trim()
  if (term.length < 2) {
    store.searchResults = []
    return
  }

  debounceHandle = setTimeout(() => {
    debounceHandle = null
    void store.search(term)
  }, SEARCH_DEBOUNCE_MS)
}

function onSelect(article: KnowledgeBaseArticle) {
  router.push({ name: 'knowledge-base-edit', params: { id: article.id } })
}
</script>

<template>
  <div class="kb-search-dialog surface" role="dialog" :aria-label="t('knowledgeBase.title')">
    <div class="kb-search-header">
      <input
        v-model="query"
        type="search"
        class="kb-search-input"
        :placeholder="t('knowledgeBase.searchPlaceholder')"
        @input="onInput"
        autofocus
      />
      <button type="button" :aria-label="t('common.close')" @click="emit('close')">
        {{ t('common.close') }}
      </button>
    </div>

    <p v-if="query.trim().length > 0 && query.trim().length < 2" class="kb-search-hint">
      {{ t('knowledgeBase.messages.searchEmpty') }}
    </p>

    <p v-else-if="store.isLoading">{{ t('common.loading') }}</p>

    <p v-else-if="query.trim().length >= 2 && store.searchResults.length === 0" class="kb-search-hint">
      {{ t('knowledgeBase.messages.searchEmpty') }}
    </p>

    <ul v-else class="kb-search-results">
      <li
        v-for="article in store.searchResults.slice(0, RESULT_LIMIT)"
        :key="article.id"
        class="kb-search-result"
        @click="onSelect(article)"
      >
        <p class="kb-search-result-title">{{ article.title }}</p>
        <p class="kb-search-result-snippet">{{ snippet(article.body) }}</p>
        <p v-if="article.tags.length > 0" class="kb-search-result-tags">{{ article.tags.join(', ') }}</p>
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

.kb-search-result-tags {
  color: var(--muted);
  font-size: var(--font-size-xs);
}
</style>
