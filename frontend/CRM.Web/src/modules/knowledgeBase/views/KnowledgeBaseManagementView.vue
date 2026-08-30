<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { useKnowledgeBaseStore } from '@/stores/knowledgeBase'
import { confirm } from '@/composables/useConfirm'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type {
  KnowledgeBaseArticle,
  KnowledgeBaseArticleCategoryRef,
  KnowledgeBaseArticleStatus,
} from '@/types/knowledgeBase'

const STATUSES: KnowledgeBaseArticleStatus[] = ['Draft', 'Published', 'Archived']
const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

const { t } = useI18n()
const route = useRoute()
const store = useKnowledgeBaseStore()

const statusFilter = ref<KnowledgeBaseArticleStatus | ''>('')
const tagFilter = ref('')

// Full-text search (CRM-66) — a separate results panel from the plain
// status/tag/category-filtered table below. Submitted explicitly on Enter
// or the Search button only; never wired to keystroke input.
// Shares the single Category dropdown in the filter row below (store.selectedCategoryId)
// rather than duplicating it, so there's only one category control on the page.
const searchInput = ref('')

function submitSearch() {
  const trimmed = searchInput.value.trim()
  if (!trimmed) {
    store.resetSearch()
    return
  }
  void store.runSearch({ query: trimmed, categoryId: store.selectedCategoryId || null, page: 1 })
}

function retrySearch() {
  void store.runSearch({
    query: store.search.query, categoryId: store.search.categoryId, page: store.search.page,
  })
}

function goToSearchPage(page: number) {
  void store.setSearchPage(page)
}

const searchTotalPages = computed(() =>
  store.search.pageSize > 0 ? Math.max(1, Math.ceil(store.search.totalCount / store.search.pageSize)) : 1,
)

const isAdding = ref(false)
const editingId = ref<string | null>(null)
const draftTitle = ref('')
const draftSlug = ref('')
const draftSlugTouched = ref(false)
const draftBody = ref('')
const draftTagsText = ref('')
const draftStatus = ref<KnowledgeBaseArticleStatus>('Draft')
const draftCategoryId = ref('')
const legacyInactiveCategory = ref<KnowledgeBaseArticleCategoryRef | null>(null)
const slugError = ref<string | null>(null)
const categoryError = ref<string | null>(null)
const conflictError = ref(false)

function slugify(title: string): string {
  return title
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

function refetch() {
  // store.fetchArticles rethrows on failure so callers that want to chain
  // can; refetch() is fire-and-forget from a click/mount handler and
  // store.error already carries the message the template displays, so the
  // rejection is swallowed here rather than left unhandled.
  store.fetchArticles({
    status: statusFilter.value || undefined,
    tag: tagFilter.value.trim() || undefined,
    categoryId: store.selectedCategoryId || undefined,
  }).catch(() => {})
}

function onCategoryFilterChange() {
  void store.setArticleCategoryFilter(store.selectedCategoryId || null)
}

onMounted(async () => {
  refetch()
  void store.fetchCategories()

  // Reached from KnowledgeBaseSearchDialog via /knowledge-base/:id — open
  // that article directly in the edit form rather than requiring the user
  // to find it again in the (possibly filtered/paged) list below.
  const routeId = route.params.id
  if (typeof routeId === 'string' && routeId) {
    try {
      const article = await store.fetchById(routeId)
      startEdit(article)
    } catch {
      // store.error already carries the message to display.
    }
  }
})

function onFilterChange() {
  refetch()
}

const displayedArticles = computed<KnowledgeBaseArticle[]>(() => store.articles)

// A deep link from KnowledgeBaseSearchDialog (/knowledge-base/:id) can open
// an article for editing that isn't part of the current filtered/paged list
// below — render its edit form standalone rather than relying on the
// per-row inline form, which only exists inside that list's v-for.
const isEditingOutsideList = computed(
  () => editingId.value !== null && !displayedArticles.value.some((a) => a.id === editingId.value),
)

function resetDraft() {
  draftTitle.value = ''
  draftSlug.value = ''
  draftSlugTouched.value = false
  draftBody.value = ''
  draftTagsText.value = ''
  draftStatus.value = 'Draft'
  draftCategoryId.value = ''
  legacyInactiveCategory.value = null
  slugError.value = null
  categoryError.value = null
  conflictError.value = false
}

function openAddForm() {
  isAdding.value = true
  editingId.value = null
  resetDraft()
}

function startEdit(article: KnowledgeBaseArticle) {
  isAdding.value = false
  editingId.value = article.id
  draftTitle.value = article.title
  draftSlug.value = article.slug
  draftSlugTouched.value = true
  draftBody.value = article.body
  draftTagsText.value = article.tags.join(', ')
  draftStatus.value = article.status
  draftCategoryId.value = article.categoryId
  // If the article's current category is inactive, keep it as a labeled,
  // pre-selected option in the picker so the manager can see and keep it
  // (or move to an active one) — but not switch back to it once they've
  // moved away, per the product rule that inactive categories are never
  // re-selectable through the picker.
  legacyInactiveCategory.value = article.category && !article.category.isActive ? article.category : null
  slugError.value = null
  categoryError.value = null
  conflictError.value = false
}

function cancelForm() {
  isAdding.value = false
  editingId.value = null
}

function onTitleInput() {
  if (!draftSlugTouched.value) {
    draftSlug.value = slugify(draftTitle.value)
  }
}

function onSlugTouched() {
  draftSlugTouched.value = true
}

function onSlugBlur() {
  const slug = draftSlug.value.trim()
  slugError.value = slug.length > 0 && !SLUG_PATTERN.test(slug)
    ? t('knowledgeBase.validation.slugFormat')
    : null
}

function parseTags(text: string): string[] {
  return text
    .split(',')
    .map((tag) => tag.trim())
    .filter((tag) => tag.length > 0)
}

const isSaveDisabled = computed(
  () => store.isLoading || draftTitle.value.trim().length === 0 || draftCategoryId.value.trim().length === 0,
)

function isDraftValid(): boolean {
  return (
    draftTitle.value.trim().length > 0 &&
    draftSlug.value.trim().length > 0 &&
    SLUG_PATTERN.test(draftSlug.value.trim()) &&
    draftCategoryId.value.trim().length > 0
  )
}

function buildPayload() {
  return {
    title: draftTitle.value.trim(),
    slug: draftSlug.value.trim(),
    body: draftBody.value,
    tags: parseTags(draftTagsText.value),
    status: draftStatus.value,
    categoryId: draftCategoryId.value,
  }
}

async function submitAdd() {
  onSlugBlur()
  if (!isDraftValid() || slugError.value) {
    return
  }
  conflictError.value = false
  try {
    await store.create(buildPayload())
    isAdding.value = false
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) {
      conflictError.value = true
    }
  }
}

async function submitEdit() {
  if (!editingId.value) {
    return
  }
  onSlugBlur()
  if (!isDraftValid() || slugError.value) {
    return
  }
  conflictError.value = false
  try {
    await store.update(editingId.value, buildPayload())
    editingId.value = null
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) {
      conflictError.value = true
    }
  }
}

async function onDelete(article: KnowledgeBaseArticle) {
  if (!(await confirm({ message: t('knowledgeBase.deleteConfirm'), tone: 'danger', confirmLabel: t('common.delete') }))) {
    return
  }
  try {
    await store.remove(article.id)
  } catch {
    // error surfaced via store.error
  }
}

const publishingId = ref<string | null>(null)

async function onPublish(article: KnowledgeBaseArticle) {
  publishingId.value = article.id
  try {
    await store.publish(article.id)
  } catch {
    // error surfaced via store.error
  } finally {
    publishingId.value = null
  }
}

async function onUnpublish(article: KnowledgeBaseArticle) {
  publishingId.value = article.id
  try {
    await store.unpublish(article.id)
  } catch {
    // error surfaced via store.error
  } finally {
    publishingId.value = null
  }
}
</script>

<template>
  <div class="knowledge-base-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('knowledgeBase.title') }}</h1>
      </div>
      <AppButton type="button" @click="openAddForm" :disabled="isAdding">
        {{ t('knowledgeBase.newArticle') }}
      </AppButton>
    </div>

    <h2 class="section-heading">{{ t('knowledgeBase.search.sectionTitle') }}</h2>
    <form class="surface toolbar" @submit.prevent="submitSearch">
      <div class="toolbar-field">
        <label for="kb-search">{{ t('common.search') }}</label>
        <input
          id="kb-search"
          v-model="searchInput"
          type="search"
          maxlength="200"
          dir="auto"
          :placeholder="t('knowledgeBase.search.placeholder')"
        />
      </div>
      <AppButton type="submit" size="sm">{{ t('knowledgeBase.search.submit') }}</AppButton>
    </form>

    <h2 class="section-heading">{{ t('knowledgeBase.filters.sectionTitle') }}</h2>
    <p class="section-hint">{{ t('knowledgeBase.filters.sharedCategoryHint') }}</p>
    <div class="surface toolbar">
      <div class="toolbar-field">
        <label for="kb-status-filter">{{ t('knowledgeBase.filters.status') }}</label>
        <select id="kb-status-filter" v-model="statusFilter" @change="onFilterChange">
          <option value="">{{ t('knowledgeBase.filters.allStatuses') }}</option>
          <option v-for="status in STATUSES" :key="status" :value="status">
            {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
          </option>
        </select>
      </div>
      <div class="toolbar-field">
        <label for="kb-tag-filter">{{ t('knowledgeBase.filters.tag') }}</label>
        <input
          id="kb-tag-filter"
          v-model="tagFilter"
          type="text"
          :placeholder="t('knowledgeBase.filters.tagPlaceholder')"
          @change="onFilterChange"
        />
      </div>
      <div class="toolbar-field">
        <label for="kb-category-filter">{{ t('knowledgeBase.filters.category') }}</label>
        <select id="kb-category-filter" v-model="store.selectedCategoryId" @change="onCategoryFilterChange">
          <option :value="null">{{ t('knowledgeBase.filters.allCategories') }}</option>
          <option v-for="category in store.categories" :key="category.id" :value="category.id">
            {{ category.name }}
          </option>
        </select>
      </div>
    </div>

    <div v-if="store.search.lastQuery" class="surface kb-search-panel">
      <h2>{{ t('knowledgeBase.search.results', { q: store.search.lastQuery }) }}</h2>
      <p>{{ t('knowledgeBase.search.count', { count: store.search.totalCount }) }}</p>

      <LoadingState v-if="store.search.loading" />
      <AppAlert v-else-if="store.search.error" tone="danger" role="alert" class="kb-error-alert">
        {{ t('knowledgeBase.search.error') }}
        <AppButton variant="secondary" size="sm" type="button" @click="retrySearch">
          {{ t('knowledgeBase.search.retry') }}
        </AppButton>
      </AppAlert>
      <EmptyState v-else-if="store.search.items.length === 0" :description="t('knowledgeBase.search.noResults')" />
      <ul v-else class="kb-search-results-list">
        <li v-for="item in store.search.items" :key="item.id" class="kb-search-result-item">
          <p class="kb-search-result-title">{{ item.title }}</p>
          <p class="kb-search-result-category">{{ item.category.name }}</p>
          <p class="kb-search-result-excerpt">{{ item.excerpt }}</p>
          <AppBadge v-if="item.status">{{ t(`knowledgeBase.status.${item.status.toLowerCase()}`) }}</AppBadge>
        </li>
      </ul>

      <div v-if="!store.search.loading && !store.search.error && searchTotalPages > 1" class="kb-search-pagination">
        <AppButton
          variant="ghost"
          size="sm"
          type="button"
          :disabled="store.search.page <= 1"
          @click="goToSearchPage(store.search.page - 1)"
        >‹</AppButton>
        <span>{{ store.search.page }} / {{ searchTotalPages }}</span>
        <AppButton
          variant="ghost"
          size="sm"
          type="button"
          :disabled="store.search.page >= searchTotalPages"
          @click="goToSearchPage(store.search.page + 1)"
        >›</AppButton>
      </div>
    </div>

    <AppAlert v-if="store.error" tone="danger" role="alert" class="kb-error-alert">
      {{ store.error }}
      <AppButton variant="secondary" size="sm" type="button" @click="refetch">{{ t('common.retry') }}</AppButton>
    </AppAlert>

    <form v-if="isAdding" class="surface kb-article-form" @submit.prevent="submitAdd">
      <div class="field">
        <label for="kb-title">{{ t('knowledgeBase.fields.title') }}</label>
        <input
          id="kb-title"
          v-model="draftTitle"
          type="text"
          maxlength="200"
          :placeholder="t('knowledgeBase.fields.titlePlaceholder')"
          @input="onTitleInput"
        />
      </div>
      <div class="field">
        <label for="kb-slug">{{ t('knowledgeBase.fields.slug') }}</label>
        <input
          id="kb-slug"
          v-model="draftSlug"
          type="text"
          maxlength="200"
          :placeholder="t('knowledgeBase.fields.slugPlaceholder')"
          @input="onSlugTouched"
          @blur="onSlugBlur"
        />
        <p v-if="slugError" role="alert" class="field-error">{{ slugError }}</p>
        <p v-if="conflictError" role="alert" class="field-error">
          {{ t('knowledgeBase.validation.slugConflict') }}
        </p>
      </div>
      <div class="field">
        <label for="kb-body">{{ t('knowledgeBase.fields.body') }}</label>
        <textarea id="kb-body" v-model="draftBody" maxlength="20000" rows="12"></textarea>
      </div>
      <div class="field">
        <label for="kb-tags">{{ t('knowledgeBase.fields.tags') }}</label>
        <input id="kb-tags" v-model="draftTagsText" type="text" :placeholder="t('knowledgeBase.fields.tagsPlaceholder')" />
      </div>
      <div class="field">
        <label for="kb-category">{{ t('knowledgeBase.fields.category') }}</label>
        <select id="kb-category" v-model="draftCategoryId">
          <option value="" disabled>{{ t('knowledgeBase.fields.categoryPlaceholder') }}</option>
          <option
            v-if="legacyInactiveCategory"
            :value="legacyInactiveCategory.id"
            disabled
          >
            {{ legacyInactiveCategory.name }} ({{ t('knowledgeBase.categories.status.inactive') }})
          </option>
          <option v-for="category in store.activeCategories" :key="category.id" :value="category.id">
            {{ category.name }}
          </option>
        </select>
        <p v-if="categoryError" role="alert" class="field-error">{{ categoryError }}</p>
      </div>
      <div class="field">
        <label for="kb-status">{{ t('knowledgeBase.fields.status') }}</label>
        <select id="kb-status" v-model="draftStatus">
          <option v-for="status in STATUSES" :key="status" :value="status">
            {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
          </option>
        </select>
      </div>
      <div class="form-actions">
        <AppButton type="submit" size="sm" :disabled="isSaveDisabled">{{ t('knowledgeBase.actions.save') }}</AppButton>
        <AppButton type="button" variant="secondary" size="sm" @click="cancelForm">{{ t('knowledgeBase.actions.cancel') }}</AppButton>
      </div>
    </form>

    <form v-else-if="isEditingOutsideList" class="surface kb-article-form" @submit.prevent="submitEdit">
      <div class="field">
        <label for="kb-title">{{ t('knowledgeBase.fields.title') }}</label>
        <input
          id="kb-title"
          v-model="draftTitle"
          type="text"
          maxlength="200"
          :placeholder="t('knowledgeBase.fields.titlePlaceholder')"
          @input="onTitleInput"
        />
      </div>
      <div class="field">
        <label for="kb-slug">{{ t('knowledgeBase.fields.slug') }}</label>
        <input
          id="kb-slug"
          v-model="draftSlug"
          type="text"
          maxlength="200"
          :placeholder="t('knowledgeBase.fields.slugPlaceholder')"
          @input="onSlugTouched"
          @blur="onSlugBlur"
        />
        <p v-if="slugError" role="alert" class="field-error">{{ slugError }}</p>
        <p v-if="conflictError" role="alert" class="field-error">
          {{ t('knowledgeBase.validation.slugConflict') }}
        </p>
      </div>
      <div class="field">
        <label for="kb-body">{{ t('knowledgeBase.fields.body') }}</label>
        <textarea id="kb-body" v-model="draftBody" maxlength="20000" rows="12"></textarea>
      </div>
      <div class="field">
        <label for="kb-tags">{{ t('knowledgeBase.fields.tags') }}</label>
        <input id="kb-tags" v-model="draftTagsText" type="text" :placeholder="t('knowledgeBase.fields.tagsPlaceholder')" />
      </div>
      <div class="field">
        <label for="kb-category">{{ t('knowledgeBase.fields.category') }}</label>
        <select id="kb-category" v-model="draftCategoryId">
          <option value="" disabled>{{ t('knowledgeBase.fields.categoryPlaceholder') }}</option>
          <option
            v-if="legacyInactiveCategory"
            :value="legacyInactiveCategory.id"
            disabled
          >
            {{ legacyInactiveCategory.name }} ({{ t('knowledgeBase.categories.status.inactive') }})
          </option>
          <option v-for="category in store.activeCategories" :key="category.id" :value="category.id">
            {{ category.name }}
          </option>
        </select>
        <p v-if="categoryError" role="alert" class="field-error">{{ categoryError }}</p>
      </div>
      <div class="field">
        <label for="kb-status">{{ t('knowledgeBase.fields.status') }}</label>
        <select id="kb-status" v-model="draftStatus">
          <option v-for="status in STATUSES" :key="status" :value="status">
            {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
          </option>
        </select>
      </div>
      <div class="form-actions">
        <AppButton type="submit" size="sm" :disabled="isSaveDisabled">{{ t('knowledgeBase.actions.save') }}</AppButton>
        <AppButton type="button" variant="secondary" size="sm" @click="cancelForm">{{ t('knowledgeBase.actions.cancel') }}</AppButton>
      </div>
    </form>

    <LoadingState v-if="store.isLoading" />
    <EmptyState v-else-if="displayedArticles.length === 0" :description="t('knowledgeBase.messages.searchEmpty')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('knowledgeBase.fields.title') }}</th>
            <th>{{ t('knowledgeBase.fields.slug') }}</th>
            <th>{{ t('knowledgeBase.fields.status') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="article in displayedArticles" :key="article.id">
            <tr v-if="editingId === article.id">
              <td colspan="4">
                <form class="kb-article-inline-form" @submit.prevent="submitEdit">
                  <input v-model="draftTitle" type="text" maxlength="200" :placeholder="t('knowledgeBase.fields.titlePlaceholder')" @input="onTitleInput" />
                  <input
                    v-model="draftSlug"
                    type="text"
                    maxlength="200"
                    :placeholder="t('knowledgeBase.fields.slugPlaceholder')"
                    @input="onSlugTouched"
                    @blur="onSlugBlur"
                  />
                  <p v-if="slugError" role="alert" class="field-error">{{ slugError }}</p>
                  <p v-if="conflictError" role="alert" class="field-error">
                    {{ t('knowledgeBase.validation.slugConflict') }}
                  </p>
                  <textarea v-model="draftBody" maxlength="20000" rows="8"></textarea>
                  <input v-model="draftTagsText" type="text" :placeholder="t('knowledgeBase.fields.tagsPlaceholder')" />
                  <select v-model="draftCategoryId">
                    <option value="" disabled>{{ t('knowledgeBase.fields.categoryPlaceholder') }}</option>
                    <option
                      v-if="legacyInactiveCategory"
                      :value="legacyInactiveCategory.id"
                      disabled
                    >
                      {{ legacyInactiveCategory.name }} ({{ t('knowledgeBase.categories.status.inactive') }})
                    </option>
                    <option v-for="category in store.activeCategories" :key="category.id" :value="category.id">
                      {{ category.name }}
                    </option>
                  </select>
                  <p v-if="categoryError" role="alert" class="field-error">{{ categoryError }}</p>
                  <select v-model="draftStatus">
                    <option v-for="status in STATUSES" :key="status" :value="status">
                      {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
                    </option>
                  </select>
                  <div class="form-actions">
                    <AppButton type="submit" size="sm" :disabled="isSaveDisabled">
                      {{ t('knowledgeBase.actions.save') }}
                    </AppButton>
                    <AppButton type="button" variant="secondary" size="sm" @click="cancelForm">{{ t('knowledgeBase.actions.cancel') }}</AppButton>
                  </div>
                </form>
              </td>
            </tr>
            <tr v-else>
              <td>{{ article.title }}</td>
              <td>{{ article.slug }}</td>
              <td>{{ t(`knowledgeBase.status.${article.status.toLowerCase()}`) }}</td>
              <td>
                <AppButton variant="ghost" size="sm" type="button" @click="startEdit(article)">{{ t('knowledgeBase.actions.edit') }}</AppButton>
                <AppButton
                  v-if="article.status !== 'Published'"
                  variant="ghost"
                  size="sm"
                  type="button"
                  :disabled="publishingId === article.id"
                  @click="onPublish(article)"
                >
                  {{ publishingId === article.id ? t('knowledgeBase.form.publishing') : t('knowledgeBase.actions.publish') }}
                </AppButton>
                <AppButton
                  v-else
                  variant="ghost"
                  size="sm"
                  type="button"
                  :disabled="publishingId === article.id"
                  @click="onUnpublish(article)"
                >
                  {{ publishingId === article.id ? t('knowledgeBase.form.unpublishing') : t('knowledgeBase.actions.unpublish') }}
                </AppButton>
                <AppButton variant="ghost" size="sm" type="button" @click="onDelete(article)">
                  {{ t('knowledgeBase.actions.delete') }}
                </AppButton>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.knowledge-base-view {
  max-width: 72rem;
  margin: var(--space-8) auto;
}

.kb-article-form,
.kb-article-inline-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-5);
  margin-bottom: var(--space-5);
}

.field-error {
  color: var(--color-status-danger);
  font-size: var(--font-size-sm);
}

.kb-error-alert {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.form-actions {
  display: flex;
  gap: var(--space-2);
}

.kb-search-panel {
  margin-bottom: var(--space-5);
  padding: var(--space-5);
}

.section-heading {
  margin: var(--space-4) 0 var(--space-2);
  font-size: 0.875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.02em;
  color: var(--color-text-muted, inherit);
}

.section-hint {
  margin: 0 0 var(--space-2);
  font-size: 0.8125rem;
  color: var(--color-text-muted, inherit);
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
</style>
