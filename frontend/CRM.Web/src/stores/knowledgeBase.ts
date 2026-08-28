import { computed, ref } from 'vue'
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
  KnowledgeBaseSearchQuery,
  UpdateKnowledgeBaseArticlePayload,
  UpdateKnowledgeBaseCategoryPayload,
} from '@/types/knowledgeBase'

const t = i18n.global.t

export const useKnowledgeBaseStore = defineStore('knowledgeBase', () => {
  const articles = ref<KnowledgeBaseArticle[]>([])
  const currentArticle = ref<KnowledgeBaseArticle | null>(null)
  const searchResults = ref<KnowledgeBaseArticle[]>([])
  const total = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

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

  async function search(q: string, filters: Omit<KnowledgeBaseSearchQuery, 'q'> = {}) {
    isLoading.value = true
    error.value = null

    try {
      const result = await searchArticles({ ...filters, q })
      searchResults.value = result.items
      total.value = result.total
      return result
    } catch (err) {
      error.value = errorMessage(err)
      throw err
    } finally {
      isLoading.value = false
    }
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
    searchResults,
    total,
    isLoading,
    error,
    fetchArticles,
    search,
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
