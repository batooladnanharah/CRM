import { reactive, ref } from 'vue'
import { defineStore } from 'pinia'
import { ApiError } from '@/api/http'
import {
  createPortalTicket,
  fetchPortalDashboard,
  fetchPortalKnowledgeBaseArticle,
  fetchPortalKnowledgeBaseArticles,
  fetchPortalTicket,
  fetchPortalTickets,
  listPortalCategories,
  listPortalCategoryArticles,
  searchPortalKnowledgeBase,
  sendPortalTicketReply,
} from '@/api/customerPortal'
import type {
  CreateCustomerTicketPayload,
  CustomerDashboard,
  CustomerKnowledgeBaseArticleDetails,
  CustomerKnowledgeBaseArticleListItem,
  CustomerKnowledgeBaseCategorySummary,
  CustomerTicketDetails,
  CustomerTicketListItem,
} from '@/types/customerPortal'
import type { KnowledgeBaseSearchItem } from '@/types/knowledgeBase'

const SEARCH_MIN_LENGTH = 2
const SEARCH_DEFAULT_PAGE_SIZE = 10

export const useCustomerPortalStore = defineStore('customerPortal', () => {
  const dashboard = ref<CustomerDashboard | null>(null)
  const tickets = ref<CustomerTicketListItem[]>([])
  const currentTicket = ref<CustomerTicketDetails | null>(null)
  const loading = ref(false)
  const creating = ref(false)
  const error = ref<string | null>(null)
  const notFound = ref(false)

  const sendingReply = ref(false)
  const replyError = ref<string | null>(null)

  const articles = ref<CustomerKnowledgeBaseArticleListItem[]>([])
  const currentArticle = ref<CustomerKnowledgeBaseArticleDetails | null>(null)
  const articlesTotal = ref(0)
  const articlesPage = ref(1)
  const articlesPageSize = ref(20)
  const articlesLoading = ref(false)
  const articlesError = ref<string | null>(null)
  const articleLoading = ref(false)
  const articleError = ref<string | null>(null)

  const portalCategories = ref<CustomerKnowledgeBaseCategorySummary[]>([])
  const portalCategoriesLoading = ref(false)
  const portalCategoriesError = ref<string | null>(null)

  const portalCategoryArticles = ref<CustomerKnowledgeBaseArticleListItem[]>([])
  const portalCategoryArticlesLoading = ref(false)
  const portalCategoryArticlesError = ref<string | null>(null)

  // Full-text search (CRM-66) — never sends includeDrafts; only ever run
  // from an explicit user submit. Guarded against stale/out-of-order
  // responses the same way as the agent-side store.
  const knowledgeBaseSearch = reactive({
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
  let knowledgeBaseSearchRequestId = 0

  async function runKnowledgeBaseSearch(
    options: { query: string; categoryId?: string | null; page?: number } = { query: '' },
  ) {
    const trimmed = options.query.trim()
    const categoryId = options.categoryId ?? null
    const page = options.page ?? 1

    knowledgeBaseSearch.query = options.query
    knowledgeBaseSearch.categoryId = categoryId
    knowledgeBaseSearch.lastQuery = trimmed

    if (trimmed.length < SEARCH_MIN_LENGTH) {
      knowledgeBaseSearch.items = []
      knowledgeBaseSearch.totalCount = 0
      knowledgeBaseSearch.error = 'tooShort'
      knowledgeBaseSearch.loading = false
      return
    }

    const requestId = ++knowledgeBaseSearchRequestId
    knowledgeBaseSearch.loading = true
    knowledgeBaseSearch.error = null

    try {
      const result = await searchPortalKnowledgeBase({
        q: trimmed, categoryId: categoryId ?? undefined, page, pageSize: knowledgeBaseSearch.pageSize,
      })

      if (requestId !== knowledgeBaseSearchRequestId) {
        return
      }

      knowledgeBaseSearch.items = result.items
      knowledgeBaseSearch.totalCount = result.totalCount
      knowledgeBaseSearch.page = result.page
      knowledgeBaseSearch.pageSize = result.pageSize
    } catch {
      if (requestId !== knowledgeBaseSearchRequestId) {
        return
      }
      knowledgeBaseSearch.error = 'errorLoad'
      knowledgeBaseSearch.items = []
      knowledgeBaseSearch.totalCount = 0
    } finally {
      if (requestId === knowledgeBaseSearchRequestId) {
        knowledgeBaseSearch.loading = false
      }
    }
  }

  function setKnowledgeBaseSearchPage(page: number) {
    return runKnowledgeBaseSearch({
      query: knowledgeBaseSearch.query, categoryId: knowledgeBaseSearch.categoryId, page,
    })
  }

  function resetKnowledgeBaseSearch() {
    knowledgeBaseSearchRequestId += 1
    knowledgeBaseSearch.query = ''
    knowledgeBaseSearch.categoryId = null
    knowledgeBaseSearch.page = 1
    knowledgeBaseSearch.items = []
    knowledgeBaseSearch.totalCount = 0
    knowledgeBaseSearch.loading = false
    knowledgeBaseSearch.error = null
    knowledgeBaseSearch.lastQuery = ''
  }

  async function fetchDashboard() {
    loading.value = true
    error.value = null

    try {
      dashboard.value = await fetchPortalDashboard()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function fetchTickets() {
    loading.value = true
    error.value = null

    try {
      tickets.value = await fetchPortalTickets()
    } catch {
      error.value = 'errorLoad'
    } finally {
      loading.value = false
    }
  }

  async function fetchTicket(id: string) {
    loading.value = true
    error.value = null
    notFound.value = false
    currentTicket.value = null

    try {
      currentTicket.value = await fetchPortalTicket(id)
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        notFound.value = true
      } else {
        error.value = 'errorLoad'
      }
    } finally {
      loading.value = false
    }
  }

  // Same fetchTicket, kept as a distinct name for a manual "refresh" action
  // in the details view so its intent reads differently from the initial
  // on-mount load, even though the behavior is identical.
  function refreshTicket(id: string) {
    return fetchTicket(id)
  }

  // Sends a customer reply and, on success, appends the returned message to
  // currentTicket.messages locally rather than re-fetching the whole ticket
  // — avoids a full reload flicker and an extra round trip. On failure the
  // caller's draft content is left untouched (this store never clears it).
  async function sendReply(id: string, body: string) {
    sendingReply.value = true
    replyError.value = null

    try {
      const message = await sendPortalTicketReply(id, { body })
      if (currentTicket.value && currentTicket.value.id === id) {
        currentTicket.value.messages.push(message)
        currentTicket.value.updatedAtUtc = message.createdAtUtc
      }
      return message
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        replyError.value = 'ticketClosed'
      } else if (err instanceof ApiError && err.status === 404) {
        replyError.value = 'notFound'
      } else {
        replyError.value = 'errorSend'
      }
      throw err
    } finally {
      sendingReply.value = false
    }
  }

  async function createTicket(payload: CreateCustomerTicketPayload) {
    creating.value = true
    error.value = null

    try {
      const created = await createPortalTicket(payload)
      return created
    } catch (err) {
      error.value = 'errorSave'
      throw err
    } finally {
      creating.value = false
    }
  }

  async function fetchArticles(page = 1, pageSize = 20) {
    articlesLoading.value = true
    articlesError.value = null

    try {
      const result = await fetchPortalKnowledgeBaseArticles(page, pageSize)
      articles.value = result.items
      articlesTotal.value = result.total
      articlesPage.value = result.page
      articlesPageSize.value = result.pageSize
    } catch {
      articlesError.value = 'errorLoad'
    } finally {
      articlesLoading.value = false
    }
  }

  async function fetchArticle(id: string) {
    articleLoading.value = true
    articleError.value = null
    currentArticle.value = null

    try {
      currentArticle.value = await fetchPortalKnowledgeBaseArticle(id)
    } catch {
      articleError.value = 'errorLoad'
    } finally {
      articleLoading.value = false
    }
  }

  async function fetchPortalCategories() {
    portalCategoriesLoading.value = true
    portalCategoriesError.value = null

    try {
      portalCategories.value = await listPortalCategories()
    } catch {
      portalCategoriesError.value = 'errorLoad'
    } finally {
      portalCategoriesLoading.value = false
    }
  }

  async function fetchPortalCategoryArticles(id: string, page = 1, pageSize = 20) {
    portalCategoryArticlesLoading.value = true
    portalCategoryArticlesError.value = null

    try {
      const result = await listPortalCategoryArticles(id, page, pageSize)
      portalCategoryArticles.value = result.items
    } catch {
      portalCategoryArticlesError.value = 'errorLoad'
    } finally {
      portalCategoryArticlesLoading.value = false
    }
  }

  return {
    dashboard,
    tickets,
    currentTicket,
    loading,
    creating,
    error,
    notFound,
    sendingReply,
    replyError,
    fetchDashboard,
    fetchTickets,
    fetchTicket,
    refreshTicket,
    sendReply,
    createTicket,
    articles,
    currentArticle,
    articlesTotal,
    articlesPage,
    articlesPageSize,
    articlesLoading,
    articlesError,
    articleLoading,
    articleError,
    fetchArticles,
    fetchArticle,
    portalCategories,
    portalCategoriesLoading,
    portalCategoriesError,
    portalCategoryArticles,
    portalCategoryArticlesLoading,
    portalCategoryArticlesError,
    fetchPortalCategories,
    fetchPortalCategoryArticles,
    knowledgeBaseSearch,
    runKnowledgeBaseSearch,
    setKnowledgeBaseSearchPage,
    resetKnowledgeBaseSearch,
  }
})
