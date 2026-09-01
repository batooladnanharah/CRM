import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import type {
  createPortalTicket,
  fetchPortalDashboard,
  fetchPortalKnowledgeBaseArticle,
  fetchPortalKnowledgeBaseArticles,
  fetchPortalTicket,
  fetchPortalTickets,
  listPortalCategories,
  listPortalCategoryArticles,
  searchPortalKnowledgeBase,
} from '@/api/customerPortal'
import type {
  CustomerDashboard,
  CustomerKnowledgeBaseArticleDetails,
  CustomerKnowledgeBaseArticleListItem,
  CustomerKnowledgeBaseCategorySummary,
  CustomerTicketDetails,
  CustomerTicketListItem,
} from '@/types/customerPortal'

const {
  fetchPortalDashboardMock,
  fetchPortalTicketsMock,
  fetchPortalTicketMock,
  createPortalTicketMock,
  fetchPortalKnowledgeBaseArticlesMock,
  fetchPortalKnowledgeBaseArticleMock,
  listPortalCategoriesMock,
  listPortalCategoryArticlesMock,
  searchPortalKnowledgeBaseMock,
} = vi.hoisted(() => ({
  fetchPortalDashboardMock: vi.fn<typeof fetchPortalDashboard>(),
  fetchPortalTicketsMock: vi.fn<typeof fetchPortalTickets>(),
  fetchPortalTicketMock: vi.fn<typeof fetchPortalTicket>(),
  createPortalTicketMock: vi.fn<typeof createPortalTicket>(),
  fetchPortalKnowledgeBaseArticlesMock: vi.fn<typeof fetchPortalKnowledgeBaseArticles>(),
  fetchPortalKnowledgeBaseArticleMock: vi.fn<typeof fetchPortalKnowledgeBaseArticle>(),
  listPortalCategoriesMock: vi.fn<typeof listPortalCategories>(),
  listPortalCategoryArticlesMock: vi.fn<typeof listPortalCategoryArticles>(),
  searchPortalKnowledgeBaseMock: vi.fn<typeof searchPortalKnowledgeBase>(),
}))

vi.mock('@/api/customerPortal', () => ({
  fetchPortalDashboard: fetchPortalDashboardMock,
  fetchPortalTickets: fetchPortalTicketsMock,
  fetchPortalTicket: fetchPortalTicketMock,
  createPortalTicket: createPortalTicketMock,
  fetchPortalKnowledgeBaseArticles: fetchPortalKnowledgeBaseArticlesMock,
  fetchPortalKnowledgeBaseArticle: fetchPortalKnowledgeBaseArticleMock,
  listPortalCategories: listPortalCategoriesMock,
  listPortalCategoryArticles: listPortalCategoryArticlesMock,
  searchPortalKnowledgeBase: searchPortalKnowledgeBaseMock,
}))

function makeTicketListItem(overrides: Partial<CustomerTicketListItem> = {}): CustomerTicketListItem {
  return {
    id: '1',
    title: 'Cannot log in',
    status: 'Open',
    priority: 'Normal',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeDashboard(overrides: Partial<CustomerDashboard> = {}): CustomerDashboard {
  return {
    openCount: 1,
    pendingCount: 2,
    resolvedCount: 3,
    recentTickets: [makeTicketListItem()],
    ...overrides,
  }
}

function makeTicketDetails(overrides: Partial<CustomerTicketDetails> = {}): CustomerTicketDetails {
  return {
    id: '1',
    title: 'Cannot log in',
    description: 'Details here.',
    status: 'Open',
    priority: 'Normal',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    messages: [],
    history: [],
    ...overrides,
  }
}

function makeArticleListItem(
  overrides: Partial<CustomerKnowledgeBaseArticleListItem> = {},
): CustomerKnowledgeBaseArticleListItem {
  return {
    id: 'a1',
    title: 'How to reset your password',
    slug: 'how-to-reset-your-password',
    tags: ['account'],
    publishedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeArticleDetails(
  overrides: Partial<CustomerKnowledgeBaseArticleDetails> = {},
): CustomerKnowledgeBaseArticleDetails {
  return {
    id: 'a1',
    title: 'How to reset your password',
    slug: 'how-to-reset-your-password',
    body: 'Full article body here.',
    tags: ['account'],
    publishedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeCategorySummary(
  overrides: Partial<CustomerKnowledgeBaseCategorySummary> = {},
): CustomerKnowledgeBaseCategorySummary {
  return {
    id: 'c1',
    name: 'Account',
    description: null,
    articleCount: 3,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  fetchPortalDashboardMock.mockReset()
  fetchPortalTicketsMock.mockReset()
  fetchPortalTicketMock.mockReset()
  createPortalTicketMock.mockReset()
  fetchPortalKnowledgeBaseArticlesMock.mockReset()
  fetchPortalKnowledgeBaseArticleMock.mockReset()
  listPortalCategoriesMock.mockReset()
  listPortalCategoryArticlesMock.mockReset()
  searchPortalKnowledgeBaseMock.mockReset()
})

describe('customerPortal store', () => {
  it('has the expected initial state', () => {
    const store = useCustomerPortalStore()

    expect(store.dashboard).toBeNull()
    expect(store.tickets).toEqual([])
    expect(store.currentTicket).toBeNull()
    expect(store.loading).toBe(false)
    expect(store.creating).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchDashboard() populates dashboard on success', async () => {
    const dashboard = makeDashboard()
    fetchPortalDashboardMock.mockResolvedValue(dashboard)

    const store = useCustomerPortalStore()
    await store.fetchDashboard()

    expect(store.dashboard).toEqual(dashboard)
    expect(store.loading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchDashboard() sets errorLoad and does not throw on failure', async () => {
    fetchPortalDashboardMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchDashboard()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.dashboard).toBeNull()
  })

  it('fetchTickets() populates tickets on success', async () => {
    const item = makeTicketListItem()
    fetchPortalTicketsMock.mockResolvedValue([item])

    const store = useCustomerPortalStore()
    await store.fetchTickets()

    expect(store.tickets).toEqual([item])
    expect(store.error).toBeNull()
  })

  it('fetchTickets() sets errorLoad and does not throw on failure', async () => {
    fetchPortalTicketsMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchTickets()).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.tickets).toEqual([])
  })

  it('fetchTicket() populates currentTicket on success', async () => {
    const details = makeTicketDetails()
    fetchPortalTicketMock.mockResolvedValue(details)

    const store = useCustomerPortalStore()
    await store.fetchTicket('1')

    expect(store.currentTicket).toEqual(details)
    expect(fetchPortalTicketMock).toHaveBeenCalledWith('1')
  })

  it('fetchTicket() sets errorLoad and clears currentTicket on failure', async () => {
    fetchPortalTicketMock.mockRejectedValue(new Error('not found'))

    const store = useCustomerPortalStore()
    await expect(store.fetchTicket('missing')).resolves.toBeUndefined()

    expect(store.error).toBe('errorLoad')
    expect(store.currentTicket).toBeNull()
  })

  it('createTicket() returns the created ticket on success', async () => {
    const details = makeTicketDetails({ id: 'new-1', title: 'New Ticket' })
    createPortalTicketMock.mockResolvedValue(details)

    const store = useCustomerPortalStore()
    const result = await store.createTicket({
      title: 'New Ticket', description: 'Something broke.', priority: 'High',
    })

    expect(result).toEqual(details)
    expect(store.creating).toBe(false)
    expect(store.error).toBeNull()
  })

  it('createTicket() sets errorSave and rethrows on failure', async () => {
    createPortalTicketMock.mockRejectedValue(new Error('failed'))

    const store = useCustomerPortalStore()
    await expect(
      store.createTicket({ title: 'Title', description: 'Description' }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorSave')
    expect(store.creating).toBe(false)
  })

  it('fetchArticles() sets loading, populates items and total on success', async () => {
    const item = makeArticleListItem()
    fetchPortalKnowledgeBaseArticlesMock.mockResolvedValue({
      items: [item], total: 1, page: 1, pageSize: 20,
    })

    const store = useCustomerPortalStore()
    const promise = store.fetchArticles()
    expect(store.articlesLoading).toBe(true)
    await promise

    expect(store.articles).toEqual([item])
    expect(store.articlesTotal).toBe(1)
    expect(store.articlesPage).toBe(1)
    expect(store.articlesPageSize).toBe(20)
    expect(store.articlesLoading).toBe(false)
    expect(store.articlesError).toBeNull()
  })

  it('fetchArticles() surfaces API errors', async () => {
    fetchPortalKnowledgeBaseArticlesMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchArticles()).resolves.toBeUndefined()

    expect(store.articlesError).toBe('errorLoad')
    expect(store.articles).toEqual([])
    expect(store.articlesLoading).toBe(false)
  })

  it('fetchArticle() populates currentArticle on success', async () => {
    const details = makeArticleDetails()
    fetchPortalKnowledgeBaseArticleMock.mockResolvedValue(details)

    const store = useCustomerPortalStore()
    await store.fetchArticle('a1')

    expect(store.currentArticle).toEqual(details)
    expect(fetchPortalKnowledgeBaseArticleMock).toHaveBeenCalledWith('a1')
    expect(store.articleError).toBeNull()
  })

  it('fetchArticle() clears currentArticle and sets errorLoad on 404', async () => {
    fetchPortalKnowledgeBaseArticleMock.mockRejectedValue(new Error('not found'))

    const store = useCustomerPortalStore()
    await expect(store.fetchArticle('missing')).resolves.toBeUndefined()

    expect(store.articleError).toBe('errorLoad')
    expect(store.currentArticle).toBeNull()
  })

  it('fetchPortalCategories() populates categories on success', async () => {
    const category = makeCategorySummary()
    listPortalCategoriesMock.mockResolvedValue([category])

    const store = useCustomerPortalStore()
    await store.fetchPortalCategories()

    expect(store.portalCategories).toEqual([category])
    expect(store.portalCategoriesError).toBeNull()
  })

  it('fetchPortalCategories() surfaces API errors', async () => {
    listPortalCategoriesMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchPortalCategories()).resolves.toBeUndefined()

    expect(store.portalCategoriesError).toBe('errorLoad')
    expect(store.portalCategories).toEqual([])
  })

  it('fetchPortalCategoryArticles() populates portalCategoryArticles on success', async () => {
    const item = makeArticleListItem()
    listPortalCategoryArticlesMock.mockResolvedValue({ items: [item], total: 1, page: 1, pageSize: 20 })

    const store = useCustomerPortalStore()
    await store.fetchPortalCategoryArticles('c1')

    expect(store.portalCategoryArticles).toEqual([item])
    expect(listPortalCategoryArticlesMock).toHaveBeenCalledWith('c1', 1, 20)
    expect(store.portalCategoryArticlesError).toBeNull()
  })

  it('fetchPortalCategoryArticles() surfaces API errors', async () => {
    listPortalCategoryArticlesMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await expect(store.fetchPortalCategoryArticles('c1')).resolves.toBeUndefined()

    expect(store.portalCategoryArticlesError).toBe('errorLoad')
    expect(store.portalCategoryArticles).toEqual([])
  })

  it('runKnowledgeBaseSearch() populates items and totalCount on success', async () => {
    searchPortalKnowledgeBaseMock.mockResolvedValue({
      items: [{ id: 'a1', title: 'How to reset your password', excerpt: '...', category: { id: 'c1', name: 'Account' }, status: null }],
      totalCount: 1,
      page: 1,
      pageSize: 10,
    })

    const store = useCustomerPortalStore()
    await store.runKnowledgeBaseSearch({ query: 'password' })

    expect(store.knowledgeBaseSearch.items).toHaveLength(1)
    expect(store.knowledgeBaseSearch.totalCount).toBe(1)
    expect(store.knowledgeBaseSearch.loading).toBe(false)
    expect(store.knowledgeBaseSearch.error).toBeNull()
  })

  it('runKnowledgeBaseSearch() sets tooShort error without calling the API for short queries', async () => {
    const store = useCustomerPortalStore()
    await store.runKnowledgeBaseSearch({ query: 'a' })

    expect(searchPortalKnowledgeBaseMock).not.toHaveBeenCalled()
    expect(store.knowledgeBaseSearch.error).toBe('tooShort')
    expect(store.knowledgeBaseSearch.items).toEqual([])
  })

  it('runKnowledgeBaseSearch() sets errorLoad and clears items on failure', async () => {
    searchPortalKnowledgeBaseMock.mockRejectedValue(new Error('network down'))

    const store = useCustomerPortalStore()
    await store.runKnowledgeBaseSearch({ query: 'password' })

    expect(store.knowledgeBaseSearch.error).toBe('errorLoad')
    expect(store.knowledgeBaseSearch.items).toEqual([])
    expect(store.knowledgeBaseSearch.totalCount).toBe(0)
  })

  it('resetKnowledgeBaseSearch() clears search state back to defaults', async () => {
    searchPortalKnowledgeBaseMock.mockResolvedValue({
      items: [{ id: 'a1', title: 'How to reset your password', excerpt: '...', category: { id: 'c1', name: 'Account' }, status: null }],
      totalCount: 1,
      page: 1,
      pageSize: 10,
    })

    const store = useCustomerPortalStore()
    await store.runKnowledgeBaseSearch({ query: 'password' })
    store.resetKnowledgeBaseSearch()

    expect(store.knowledgeBaseSearch.query).toBe('')
    expect(store.knowledgeBaseSearch.items).toEqual([])
    expect(store.knowledgeBaseSearch.totalCount).toBe(0)
    expect(store.knowledgeBaseSearch.error).toBeNull()
  })
})
