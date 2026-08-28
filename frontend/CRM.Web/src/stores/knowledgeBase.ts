import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createArticle,
  deleteArticle,
  getArticle,
  getArticleBySlug,
  listArticles,
  publishArticle,
  searchArticles,
  unpublishArticle,
  updateArticle,
} from '@/api/knowledgeBase'
import { ApiError } from '@/api/http'
import { i18n } from '@/i18n'
import { useToast } from '@/composables/useToast'
import type {
  CreateKnowledgeBaseArticlePayload,
  KnowledgeBaseArticle,
  KnowledgeBaseListQuery,
  KnowledgeBaseSearchQuery,
  UpdateKnowledgeBaseArticlePayload,
} from '@/types/knowledgeBase'

const t = i18n.global.t

export const useKnowledgeBaseStore = defineStore('knowledgeBase', () => {
  const articles = ref<KnowledgeBaseArticle[]>([])
  const currentArticle = ref<KnowledgeBaseArticle | null>(null)
  const searchResults = ref<KnowledgeBaseArticle[]>([])
  const total = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

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
  }
})
