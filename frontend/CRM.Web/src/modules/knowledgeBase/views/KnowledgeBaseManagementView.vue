<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { useKnowledgeBaseStore } from '@/stores/knowledgeBase'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { KnowledgeBaseArticle, KnowledgeBaseArticleStatus } from '@/types/knowledgeBase'

const STATUSES: KnowledgeBaseArticleStatus[] = ['Draft', 'Published', 'Archived']
const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/
const SEARCH_DEBOUNCE_MS = 300

const { t } = useI18n()
const route = useRoute()
const store = useKnowledgeBaseStore()

const statusFilter = ref<KnowledgeBaseArticleStatus | ''>('')
const tagFilter = ref('')
const searchQuery = ref('')
let searchDebounceHandle: ReturnType<typeof setTimeout> | null = null

const isAdding = ref(false)
const editingId = ref<string | null>(null)
const draftTitle = ref('')
const draftSlug = ref('')
const draftSlugTouched = ref(false)
const draftBody = ref('')
const draftTagsText = ref('')
const draftStatus = ref<KnowledgeBaseArticleStatus>('Draft')
const slugError = ref<string | null>(null)
const conflictError = ref(false)

function slugify(title: string): string {
  return title
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

function refetch() {
  void store.fetchArticles({
    status: statusFilter.value || undefined,
    tag: tagFilter.value.trim() || undefined,
  })
}

onMounted(async () => {
  refetch()

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

function onSearchInput() {
  if (searchDebounceHandle) {
    clearTimeout(searchDebounceHandle)
  }
  const term = searchQuery.value.trim()
  if (term.length < 2) {
    searchDebounceHandle = null
    refetch()
    return
  }
  searchDebounceHandle = setTimeout(() => {
    searchDebounceHandle = null
    void store.search(term, {
      status: statusFilter.value || undefined,
      tag: tagFilter.value.trim() || undefined,
    })
  }, SEARCH_DEBOUNCE_MS)
}

const displayedArticles = computed<KnowledgeBaseArticle[]>(() =>
  searchQuery.value.trim().length >= 2 ? store.searchResults : store.articles,
)

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
  slugError.value = null
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
  slugError.value = null
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

function isDraftValid(): boolean {
  return (
    draftTitle.value.trim().length > 0 &&
    draftSlug.value.trim().length > 0 &&
    SLUG_PATTERN.test(draftSlug.value.trim())
  )
}

function buildPayload() {
  return {
    title: draftTitle.value.trim(),
    slug: draftSlug.value.trim(),
    body: draftBody.value,
    tags: parseTags(draftTagsText.value),
    status: draftStatus.value,
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
  if (!window.confirm(t('knowledgeBase.deleteConfirm'))) {
    return
  }
  try {
    await store.remove(article.id)
  } catch {
    // error surfaced via store.error
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

    <div class="surface toolbar">
      <div class="toolbar-field">
        <label for="kb-search">{{ t('common.search') }}</label>
        <input
          id="kb-search"
          v-model="searchQuery"
          type="search"
          :placeholder="t('knowledgeBase.searchPlaceholder')"
          @input="onSearchInput"
        />
      </div>
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
    </div>

    <AppAlert v-if="store.error" tone="danger" role="alert">{{ store.error }}</AppAlert>

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
        <label for="kb-status">{{ t('knowledgeBase.fields.status') }}</label>
        <select id="kb-status" v-model="draftStatus">
          <option v-for="status in STATUSES" :key="status" :value="status">
            {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
          </option>
        </select>
      </div>
      <div class="form-actions">
        <AppButton type="submit" size="sm" :disabled="store.isLoading">{{ t('knowledgeBase.actions.save') }}</AppButton>
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
        <label for="kb-status">{{ t('knowledgeBase.fields.status') }}</label>
        <select id="kb-status" v-model="draftStatus">
          <option v-for="status in STATUSES" :key="status" :value="status">
            {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
          </option>
        </select>
      </div>
      <div class="form-actions">
        <AppButton type="submit" size="sm" :disabled="store.isLoading">{{ t('knowledgeBase.actions.save') }}</AppButton>
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
                  <select v-model="draftStatus">
                    <option v-for="status in STATUSES" :key="status" :value="status">
                      {{ t(`knowledgeBase.status.${status.toLowerCase()}`) }}
                    </option>
                  </select>
                  <div class="form-actions">
                    <AppButton type="submit" size="sm" :disabled="store.isLoading">
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

.form-actions {
  display: flex;
  gap: var(--space-2);
}
</style>
