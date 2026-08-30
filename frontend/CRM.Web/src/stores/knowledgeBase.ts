import { computed, reactive, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createArticle,
  createCategory as createCategoryApi,
  deleteArticle,
  getArticle,
  getArticleBySlug,
  listArticles,
  listCategories,
  publishArticle,
  searchArticles,
  setCategoryStatus,
  unpublishArticle,
  updateArticle,
  updateCategory as updateCategoryApi,
} from '@/api/knowledgeBase'
import { ApiError } from '@/api/http'
import { i18n } from '@/i18n'
import { useToast } from '@/composables/useToast'
import type {
  CreateKnowledgeBaseArticlePayload,
  CreateKnowledgeBaseCategoryPayload,
  KnowledgeBaseArticle,
  KnowledgeBaseCategory,
  KnowledgeBaseListQuery,
  KnowledgeBaseSearchItem,
  UpdateKnowledgeBaseArticlePayload,
  UpdateKnowledgeBaseCategoryPayload,
} from '@/types/knowledgeBase'

const SEARCH_MIN_LENGTH = 2
const SEARCH_DEFAULT_PAGE_SIZE = 10

const t = i18n.global.t

export const useKnowledgeBaseStore = defineStore('knowledgeBase', () => {
  const articles = ref<KnowledgeBaseArticle[]>([])
  const currentArticle = ref<KnowledgeBaseArticle | null>(null)
  const total = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Full-text search state (CRM-66), kept separate from the plain
  // status/tag/category-filtered `articles` list above. `search.lastQuery`
  // is only set once a search has actually been submitted, so the UI can
  // distinguish "no search run yet" from "search ran, zero results".
  const search = reactive({
    query: '',
    categoryId: null as string | null,
    page: 1,
    pageSize: SEARCH_DEFAULT_PAGE_SIZE,
    items: [] as KnowledgeBaseSearchItem[],
    totalCount: 0,
    loading: false,
    error: null as string | null,
    lastQuery: '',
  })

  // Monotonically increasing token: a slow/stale runSearch response is
  // discarded if a newer search has been kicked off in the meantime (fast
  // Enter presses / rapid re-submits should never let an out-of-order
  // response clobber a fresher one).
  let searchRequestId = 0

  const categories = ref<KnowledgeBaseCategory[]>([])
  const categoriesLoading = ref(false)
  const categoriesError = ref<string | null>(null)
  const selectedCategoryId = ref<string | null>(null)

  const activeCategories = computed(() => categories.value.filter((c) => c.isActive === true))

  function errorMessage(err: unknown): string {
    return err instanceof ApiError ? err.message : 'errorLoad'
  }

  async function fetchArticles(query: KnowledgeBaseListQuery = {}) {
    isLoading.value = true
    error.value = null

    try {
      const result = await listArticles(query)
      articles.value = result.items
      total.value = result.total
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  // Full-text search (CRM-66). Only ever invoked from an explicit user
  // submit (Enter / Search button) — never wired to keystroke input — and
  // guarded against out-of-order responses via searchRequestId: if a newer
  // runSearch call has started by the time this one resolves, its result is
  // discarded rather than overwriting the fresher state.
  async function runSearch(options: { query: string; categoryId?: string | null; page?: number } = { query: '' }) {
    const trimmed = options.query.trim()
    const categoryId = options.categoryId ?? null
    const page = options.page ?? 1

    search.query = options.query
    search.categoryId = categoryId
    search.lastQuery = trimmed

    if (trimmed.length < SEARCH_MIN_LENGTH) {
      search.items = []
      search.totalCount = 0
      search.error = 'tooShort'
      search.loading = false
      return
    }

    const requestId = ++searchRequestId
    search.loading = true
    search.error = null

    try {
      const result = await searchArticles({
        q: trimmed,
        categoryId: categoryId ?? undefined,
        page,
        pageSize: search.pageSize,
      })

      if (requestId !== searchRequestId) {
        // A newer search was started while this one was in flight.
        return
      }

      search.items = result.items
      search.totalCount = result.totalCount
      search.page = result.page
      search.pageSize = result.pageSize
    } catch (err) {
      if (requestId !== searchRequestId) {
        return
      }
      search.error = errorMessage(err)
      search.items = []
      search.totalCount = 0
    } finally {
      if (requestId === searchRequestId) {
        search.loading = false
      }
    }
  }

  function setSearchPage(page: number) {
    return runSearch({ query: search.query, categoryId: search.categoryId, page })
  }

  function resetSearch() {
    searchRequestId += 1 // invalidate any in-flight request
    search.query = ''
    search.categoryId = null
    search.page = 1
    search.items = []
    search.totalCount = 0
    search.loading = false
    search.error = null
    search.lastQuery = ''
  }

  async function fetchById(id: string) {
    isLoading.value = true
    error.value = null

    try {
      const article = await getArticle(id)
      currentArticle.value = article
      return article
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function fetchBySlug(slug: string) {
    isLoading.value = true
    error.value = null

    try {
      const article = await getArticleBySlug(slug)
      currentArticle.value = article
      return article
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function create(payload: CreateKnowledgeBaseArticlePayload) {
    isLoading.value = true
    error.value = null

    try {
      const created = await createArticle(payload)
      articles.value = [...articles.value, created].sort((a, b) => a.title.localeCompare(b.title))
      useToast().success(t('notifications.knowledgeBase.articleCreated'))
      return created
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function update(id: string, payload: UpdateKnowledgeBaseArticlePayload) {
    isLoading.value = true
    error.value = null

    try {
      const updated = await updateArticle(id, payload)
      articles.value = articles.value.map((a) => (a.id === id ? updated : a))
      if (currentArticle.value?.id === id) {
        currentArticle.value = updated
      }
      useToast().success(t('notifications.knowledgeBase.articleUpdated'))
      return updated
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function publish(id: string) {
    isLoading.value = true
    error.value = null

    try {
      const updated = await publishArticle(id)
      articles.value = articles.value.map((a) => (a.id === id ? updated : a))
      if (currentArticle.value?.id === id) {
        currentArticle.value = updated
      }
      useToast().success(t('notifications.knowledgeBase.articlePublished'))
      return updated
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function unpublish(id: string) {
    isLoading.value = true
    error.value = null

    try {
      const updated = await unpublishArticle(id)
      articles.value = articles.value.map((a) => (a.id === id ? updated : a))
      if (currentArticle.value?.id === id) {
        currentArticle.value = updated
      }
      useToast().success(t('notifications.knowledgeBase.articleUnpublished'))
      return updated
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function remove(id: string) {
    isLoading.value = true
    error.value = null

    try {
      await deleteArticle(id)
      articles.value = articles.value.filter((a) => a.id !== id)
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function fetchCategories(query: { activeOnly?: boolean } = {}) {
    categoriesLoading.value = true
    categoriesError.value = null

    try {
      categories.value = await listCategories(query)
    } catch (err) {
      categoriesError.value = errorMessage(err)
      throw err
    } finally {
      categoriesLoading.value = false
    }
  }

  async function createCategory(payload: CreateKnowledgeBaseCategoryPayload) {
    categoriesLoading.value = true
    categoriesError.value = null

    try {
      const created = await createCategoryApi(payload)
      categories.value = [...categories.value, created].sort((a, b) => a.name.localeCompare(b.name))
      return created
    } catch (err) {
      categoriesError.value = errorMessage(err)
      throw err
    } finally {
      categoriesLoading.value = false
    }
  }

  async function updateCategory(id: string, payload: UpdateKnowledgeBaseCategoryPayload) {
    categoriesLoading.value = true
    categoriesError.value = null

    try {
      const updated = await updateCategoryApi(id, payload)
      categories.value = categories.value.map((c) => (c.id === id ? updated : c))
      return updated
    } catch (err) {
      categoriesError.value = errorMessage(err)
      throw err
    } finally {
      categoriesLoading.value = false
    }
  }

  async function setCategoryActive(id: string, isActive: boolean) {
    categoriesLoading.value = true
    categoriesError.value = null

    try {
      const updated = await setCategoryStatus(id, isActive)
      categories.value = categories.value.map((c) => (c.id === id ? updated : c))
      return updated
    } catch (err) {
      categoriesError.value = errorMessage(err)
      throw err
    } finally {
      categoriesLoading.value = false
    }
  }

  function activateCategory(id: string) {
    return setCategoryActive(id, true)
  }

  function deactivateCategory(id: string) {
    return setCategoryActive(id, false)
  }

  function setArticleCategoryFilter(id: string | null) {
    selectedCategoryId.value = id
    return fetchArticles({
      categoryId: id ?? undefined,
    }).catch(() => {})
  }

  return {
    articles,
    currentArticle,
    total,
    isLoading,
    error,
    search,
    runSearch,
    setSearchPage,
    resetSearch,
    fetchArticles,
    fetchById,
    fetchBySlug,
    create,
    update,
    publish,
    unpublish,
    remove,
    categories,
    categoriesLoading,
    categoriesError,
    selectedCategoryId,
    activeCategories,
    fetchCategories,
    createCategory,
    updateCategory,
    activateCategory,
    deactivateCategory,
    setArticleCategoryFilter,
  }
})
