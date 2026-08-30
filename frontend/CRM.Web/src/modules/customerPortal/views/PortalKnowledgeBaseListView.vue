<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const { t } = useI18n()
const router = useRouter()
const store = useCustomerPortalStore()

function loadCategories() {
  void store.fetchPortalCategories()
}

function onCardClick(id: string) {
  router.push({ name: 'portal-knowledge-base-category', params: { id } })
}

onMounted(loadCategories)

// Full-text search (CRM-66) — submit on Enter/button only, never wired to
// keystroke input. Never sends includeDrafts (the store/api layer never
// exposes that flag for portal search).
const searchInput = ref('')

function submitSearch() {
  const trimmed = searchInput.value.trim()
  if (!trimmed) {
    store.resetKnowledgeBaseSearch()
    return
  }
  void store.runKnowledgeBaseSearch({ query: trimmed, page: 1 })
}

function retrySearch() {
  void store.runKnowledgeBaseSearch({
    query: store.knowledgeBaseSearch.query, categoryId: store.knowledgeBaseSearch.categoryId,
    page: store.knowledgeBaseSearch.page,
  })
}

function goToSearchPage(page: number) {
  void store.setKnowledgeBaseSearchPage(page)
}

function onResultClick(id: string) {
  router.push({ name: 'portal-knowledge-base-article', params: { id } })
}

function onContactSupport() {
  router.push({ name: 'portal-ticket-create' })
}

const searchTotalPages = computed(() => {
  const pageSize = store.knowledgeBaseSearch.pageSize
  return pageSize > 0 ? Math.max(1, Math.ceil(store.knowledgeBaseSearch.totalCount / pageSize)) : 1
})
</script>

<template>
  <div class="portal-kb-list-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('portal.dashboard.overline') }}</p>
        <h1>{{ t('portal.helpCentre.title') }}</h1>
      </div>
    </div>

    <form class="surface portal-search-form" @submit.prevent="submitSearch">
      <input
        v-model="searchInput"
        type="search"
        maxlength="200"
        dir="auto"
        :placeholder="t('knowledgeBase.search.portalPlaceholder')"
      />
      <AppButton type="submit" size="sm">{{ t('knowledgeBase.search.submit') }}</AppButton>
    </form>

    <div v-if="store.knowledgeBaseSearch.lastQuery" class="surface kb-search-panel">
      <h2>{{ t('knowledgeBase.search.results', { q: store.knowledgeBaseSearch.lastQuery }) }}</h2>
      <p>{{ t('knowledgeBase.search.count', { count: store.knowledgeBaseSearch.totalCount }) }}</p>

      <LoadingState v-if="store.knowledgeBaseSearch.loading" />
      <AppAlert v-else-if="store.knowledgeBaseSearch.error" tone="danger" role="alert" class="portal-error">
        {{ t('knowledgeBase.search.error') }}
        <AppButton variant="secondary" size="sm" type="button" @click="retrySearch">
          {{ t('knowledgeBase.search.retry') }}
        </AppButton>
      </AppAlert>
      <div v-else-if="store.knowledgeBaseSearch.items.length === 0" class="kb-search-no-results">
        <EmptyState :description="t('knowledgeBase.search.noResults')" />
        <AppButton variant="secondary" size="sm" type="button" @click="onContactSupport">
          {{ t('knowledgeBase.search.contactSupport') }}
        </AppButton>
      </div>
      <ul v-else class="kb-search-results-list">
        <li
          v-for="item in store.knowledgeBaseSearch.items"
          :key="item.id"
          class="kb-search-result-item"
          @click="onResultClick(item.id)"
        >
          <p class="kb-search-result-title" dir="auto">{{ item.title }}</p>
          <p class="kb-search-result-category">{{ item.category.name }}</p>
          <p class="kb-search-result-excerpt" dir="auto">{{ item.excerpt }}</p>
        </li>
      </ul>

      <div
        v-if="!store.knowledgeBaseSearch.loading && !store.knowledgeBaseSearch.error && searchTotalPages > 1"
        class="kb-search-pagination"
      >
        <AppButton
          variant="ghost"
          size="sm"
          type="button"
          :disabled="store.knowledgeBaseSearch.page <= 1"
          @click="goToSearchPage(store.knowledgeBaseSearch.page - 1)"
        >‹</AppButton>
        <span>{{ store.knowledgeBaseSearch.page }} / {{ searchTotalPages }}</span>
        <AppButton
          variant="ghost"
          size="sm"
          type="button"
          :disabled="store.knowledgeBaseSearch.page >= searchTotalPages"
          @click="goToSearchPage(store.knowledgeBaseSearch.page + 1)"
        >›</AppButton>
      </div>
    </div>

    <LoadingState v-if="store.portalCategoriesLoading" :label="t('portal.helpCentre.loading')" />
    <AppAlert v-else-if="store.portalCategoriesError" tone="danger" class="portal-error">
      {{ t('portal.helpCentre.error') }}
      <AppButton variant="secondary" size="sm" type="button" @click="loadCategories">
        {{ t('portal.helpCentre.retry') }}
      </AppButton>
    </AppAlert>
    <EmptyState v-else-if="store.portalCategories.length === 0" :title="t('knowledgeBase.categories.emptyPortal')" />

    <div v-else class="category-grid">
      <button
        v-for="category in store.portalCategories"
        :key="category.id"
        type="button"
        class="surface category-card"
        @click="onCardClick(category.id)"
      >
        <h3 dir="auto">{{ category.name }}</h3>
        <p v-if="category.description" class="category-card-description" dir="auto">{{ category.description }}</p>
        <p class="category-card-count">
          {{ t('knowledgeBase.categories.articleCount', { count: category.articleCount }) }}
        </p>
      </button>
    </div>
  </div>
</template>

<style scoped>
.portal-kb-list-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.portal-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.portal-search-form {
  display: flex;
  gap: var(--space-2);
  padding: var(--space-4);
  margin-bottom: var(--space-4);
}

.portal-search-form input {
  flex: 1;
}

.kb-search-panel {
  margin-bottom: var(--space-5);
  padding: var(--space-5);
}

.kb-search-no-results {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-3);
}

.kb-search-results-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.kb-search-result-item {
  cursor: pointer;
  padding: var(--space-3);
  border-radius: var(--radius-sm);
}

.kb-search-result-title {
  font-weight: 700;
}

.kb-search-result-category,
.kb-search-result-excerpt {
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.kb-search-pagination {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-top: var(--space-3);
}

.category-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr));
  gap: var(--space-4);
}

.category-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-2);
  padding: var(--space-5);
  text-align: start;
  cursor: pointer;
}

.category-card-description {
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.category-card-count {
  font-weight: 600;
}
</style>
