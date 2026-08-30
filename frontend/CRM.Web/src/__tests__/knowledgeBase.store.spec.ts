import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useKnowledgeBaseStore } from '@/stores/knowledgeBase'
import { ApiError } from '@/api/http'
import type {
  createArticle,
  createCategory,
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
  updateCategory,
} from '@/api/knowledgeBase'
import type { KnowledgeBaseArticle, KnowledgeBaseCategory, KnowledgeBaseSearchItem } from '@/types/knowledgeBase'

const {
  listArticlesMock,
  searchArticlesMock,
  getArticleMock,
  getArticleBySlugMock,
  createArticleMock,
  updateArticleMock,
  deleteArticleMock,
  publishArticleMock,
  unpublishArticleMock,
  listCategoriesMock,
  createCategoryMock,
  updateCategoryMock,
  setCategoryStatusMock,
  toastMock,
} = vi.hoisted(() => ({
  listArticlesMock: vi.fn<typeof listArticles>(),
  searchArticlesMock: vi.fn<typeof searchArticles>(),
  getArticleMock: vi.fn<typeof getArticle>(),
  getArticleBySlugMock: vi.fn<typeof getArticleBySlug>(),
  createArticleMock: vi.fn<typeof createArticle>(),
  updateArticleMock: vi.fn<typeof updateArticle>(),
  deleteArticleMock: vi.fn<typeof deleteArticle>(),
  publishArticleMock: vi.fn<typeof publishArticle>(),
  unpublishArticleMock: vi.fn<typeof unpublishArticle>(),
  listCategoriesMock: vi.fn<typeof listCategories>(),
  createCategoryMock: vi.fn<typeof createCategory>(),
  updateCategoryMock: vi.fn<typeof updateCategory>(),
  setCategoryStatusMock: vi.fn<typeof setCategoryStatus>(),
  toastMock: {
    success: vi.fn<(input: unknown) => string>(),
    error: vi.fn<(input: unknown) => string>(),
    warning: vi.fn<(input: unknown) => string>(),
    info: vi.fn<(input: unknown) => string>(),
    dismiss: vi.fn<(id: string) => void>(),
    clear: vi.fn<() => void>(),
  },
}))

vi.mock('@/composables/useToast', () => ({ useToast: () => toastMock }))

vi.mock('@/api/knowledgeBase', () => ({
  listArticles: listArticlesMock,
  searchArticles: searchArticlesMock,
  getArticle: getArticleMock,
  getArticleBySlug: getArticleBySlugMock,
  createArticle: createArticleMock,
  updateArticle: updateArticleMock,
  deleteArticle: deleteArticleMock,
  publishArticle: publishArticleMock,
  unpublishArticle: unpublishArticleMock,
  listCategories: listCategoriesMock,
  createCategory: createCategoryMock,
  updateCategory: updateCategoryMock,
  setCategoryStatus: setCategoryStatusMock,
}))

function makeArticle(overrides: Partial<KnowledgeBaseArticle> = {}): KnowledgeBaseArticle {
  return {
    id: '1',
    title: 'Resetting Your Password',
    slug: 'resetting-your-password',
    body: 'Steps to reset your password.',
    tags: ['account'],
    status: 'Draft',
    authorId: 'user-1',
    categoryId: 'cat-1',
    category: { id: 'cat-1', name: 'Account', isActive: true },
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    publishedAtUtc: null,
    ...overrides,
  }
}

function makeSearchItem(overrides: Partial<KnowledgeBaseSearchItem> = {}): KnowledgeBaseSearchItem {
  return {
    id: '1',
    title: 'Resetting Your Password',
    category: { id: 'cat-1', name: 'Account' },
    excerpt: 'Steps to reset your password.',
    status: null,
    ...overrides,
  }
}

function makeCategory(overrides: Partial<KnowledgeBaseCategory> = {}): KnowledgeBaseCategory {
  return {
    id: 'cat-1',
    name: 'Account',
    description: null,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  listArticlesMock.mockReset()
  searchArticlesMock.mockReset()
  getArticleMock.mockReset()
  getArticleBySlugMock.mockReset()
  createArticleMock.mockReset()
  updateArticleMock.mockReset()
  deleteArticleMock.mockReset()
  publishArticleMock.mockReset()
  unpublishArticleMock.mockReset()
  listCategoriesMock.mockReset()
  createCategoryMock.mockReset()
  updateCategoryMock.mockReset()
  setCategoryStatusMock.mockReset()
  toastMock.success.mockReset()
  toastMock.error.mockReset()
})

describe('knowledgeBase store', () => {
  it('has the expected initial state', () => {
    const store = useKnowledgeBaseStore()

    expect(store.articles).toEqual([])
    expect(store.currentArticle).toBeNull()
    expect(store.search.items).toEqual([])
    expect(store.search.lastQuery).toBe('')
    expect(store.total).toBe(0)
    expect(store.isLoading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchArticles() populates articles and total on success', async () => {
    const article = makeArticle()
    listArticlesMock.mockResolvedValue({ items: [article], total: 1 })

    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    expect(store.articles).toEqual([article])
    expect(store.total).toBe(1)
    expect(store.isLoading).toBe(false)
    expect(store.error).toBeNull()
  })

  it('fetchArticles() sets error and rethrows on failure', async () => {
    listArticlesMock.mockRejectedValue(new Error('network down'))

    const store = useKnowledgeBaseStore()
    await expect(store.fetchArticles()).rejects.toThrow('network down')

    expect(store.error).toBe('errorLoad')
    expect(store.isLoading).toBe(false)
  })

  it('runSearch() trims the query, calls the API, and populates state', async () => {
    const item = makeSearchItem({ title: 'Zzyzx Match' })
    searchArticlesMock.mockResolvedValue({ items: [item], page: 1, pageSize: 10, totalCount: 1 })

    const store = useKnowledgeBaseStore()
    await store.runSearch({ query: '  zzyzx  ' })

    expect(searchArticlesMock).toHaveBeenCalledWith({ q: 'zzyzx', categoryId: undefined, page: 1, pageSize: 10 })
    expect(store.search.items).toEqual([item])
    expect(store.search.totalCount).toBe(1)
    expect(store.search.lastQuery).toBe('zzyzx')
  })

  it('runSearch() rejects a query shorter than 2 characters without calling the API', async () => {
    const store = useKnowledgeBaseStore()
    await store.runSearch({ query: 'a' })

    expect(searchArticlesMock).not.toHaveBeenCalled()
    expect(store.search.items).toEqual([])
    expect(store.search.error).toBe('tooShort')
  })

  it('runSearch() sets loading true while in flight and false on success', async () => {
    let resolveFn!: (value: { items: KnowledgeBaseSearchItem[]; page: number; pageSize: number; totalCount: number }) => void
    searchArticlesMock.mockReturnValue(new Promise((resolve) => { resolveFn = resolve }))

    const store = useKnowledgeBaseStore()
    const pending = store.runSearch({ query: 'password' })

    expect(store.search.loading).toBe(true)
    resolveFn({ items: [], page: 1, pageSize: 10, totalCount: 0 })
    await pending

    expect(store.search.loading).toBe(false)
  })

  it('runSearch() surfaces an error message on API failure', async () => {
    searchArticlesMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await store.runSearch({ query: 'term' })

    expect(store.search.error).toBe('errorLoad')
    expect(store.search.items).toEqual([])
  })

  it('ignores a stale response when a newer runSearch has already been issued', async () => {
    let resolveFirst!: (value: { items: KnowledgeBaseSearchItem[]; page: number; pageSize: number; totalCount: number }) => void
    searchArticlesMock.mockImplementationOnce(
      () => new Promise((resolve) => { resolveFirst = resolve }),
    )

    const store = useKnowledgeBaseStore()
    const firstCall = store.runSearch({ query: 'first' })

    searchArticlesMock.mockResolvedValueOnce({
      items: [makeSearchItem({ id: 'second', title: 'Second Result' })], page: 1, pageSize: 10, totalCount: 1,
    })
    const secondCall = store.runSearch({ query: 'second' })
    await secondCall

    expect(store.search.items[0]?.id).toBe('second')

    // The stale first response resolves after the second one already landed
    // — it must not overwrite the fresher state.
    resolveFirst({ items: [makeSearchItem({ id: 'first', title: 'First Result' })], page: 1, pageSize: 10, totalCount: 1 })
    await firstCall

    expect(store.search.items[0]?.id).toBe('second')
  })

  it('fetchById() sets currentArticle on success', async () => {
    const article = makeArticle()
    getArticleMock.mockResolvedValue(article)

    const store = useKnowledgeBaseStore()
    await store.fetchById('1')

    expect(store.currentArticle).toEqual(article)
  })

  it('fetchBySlug() sets currentArticle on success', async () => {
    const article = makeArticle()
    getArticleBySlugMock.mockResolvedValue(article)

    const store = useKnowledgeBaseStore()
    await store.fetchBySlug('resetting-your-password')

    expect(store.currentArticle).toEqual(article)
  })

  it('create() appends the created article, sorted by title', async () => {
    listArticlesMock.mockResolvedValue({ items: [], total: 0 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    createArticleMock.mockResolvedValue(makeArticle({ id: '2', title: 'Apple Article' }))
    const result = await store.create({
      title: 'Apple Article', slug: 'apple-article', body: 'Body', tags: [], status: 'Draft', categoryId: 'cat-1',
    })

    expect(result.title).toBe('Apple Article')
    expect(store.articles.map((a) => a.title)).toEqual(['Apple Article'])
    expect(toastMock.success).toHaveBeenCalledTimes(1)
  })

  it('create() sets error and rethrows on failure', async () => {
    createArticleMock.mockRejectedValue(new ApiError(409, 'slug_conflict'))

    const store = useKnowledgeBaseStore()
    await expect(
      store.create({ title: 'Title', slug: 'slug', body: 'Body', tags: [], status: 'Draft', categoryId: 'cat-1' }),
    ).rejects.toThrow('slug_conflict')

    expect(store.error).toBe('slug_conflict')
  })

  it('update() replaces the article in place on success', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', title: 'Original' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    const updated = makeArticle({ id: '1', title: 'Updated' })
    updateArticleMock.mockResolvedValue(updated)
    await store.update('1', { title: 'Updated', slug: 'resetting-your-password', body: 'Body', tags: [], status: 'Draft', categoryId: 'cat-1' })

    expect(store.articles[0]).toEqual(updated)
    expect(toastMock.success).toHaveBeenCalledTimes(1)
  })

  it('update() sets error and rethrows on failure', async () => {
    updateArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(
      store.update('1', { title: 'Title', slug: 'slug', body: 'Body', tags: [], status: 'Draft', categoryId: 'cat-1' }),
    ).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
  })

  it('publish() calls the publish API and replaces the article in place', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', status: 'Draft' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    const published = makeArticle({ id: '1', status: 'Published', publishedAtUtc: '2026-01-02T00:00:00Z' })
    publishArticleMock.mockResolvedValue(published)
    await store.publish('1')

    expect(publishArticleMock).toHaveBeenCalledWith('1')
    expect(store.articles[0]).toEqual(published)
    expect(toastMock.success).toHaveBeenCalledTimes(1)
  })

  it('publish() sets error and rethrows on failure', async () => {
    publishArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(store.publish('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
  })

  it('unpublish() calls the unpublish API and updates the article status to Draft', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', status: 'Published' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    const unpublished = makeArticle({ id: '1', status: 'Draft' })
    unpublishArticleMock.mockResolvedValue(unpublished)
    await store.unpublish('1')

    expect(unpublishArticleMock).toHaveBeenCalledWith('1')
    expect(store.articles[0]?.status).toBe('Draft')
  })

  it('unpublish() sets error and rethrows on failure', async () => {
    unpublishArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(store.unpublish('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
  })

  it('remove() removes the article from state on success', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    deleteArticleMock.mockResolvedValue(undefined)
    await store.remove('1')

    expect(store.articles).toEqual([])
  })

  it('remove() sets error and rethrows on failure', async () => {
    deleteArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(store.remove('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
  })

  it('publish() calls the publish API and updates the local article status', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', status: 'Draft' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    const published = makeArticle({ id: '1', status: 'Published', publishedAtUtc: '2026-01-02T00:00:00Z' })
    publishArticleMock.mockResolvedValue(published)

    const result = await store.publish('1')

    expect(publishArticleMock).toHaveBeenCalledWith('1')
    expect(result.status).toBe('Published')
    expect(store.articles[0]).toEqual(published)
    expect(toastMock.success).toHaveBeenCalledTimes(1)
  })

  it('publish() sets error and rethrows on failure', async () => {
    publishArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(store.publish('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
  })

  it('unpublish() calls the unpublish API and updates status to Draft', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', status: 'Published' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    const unpublished = makeArticle({ id: '1', status: 'Draft' })
    unpublishArticleMock.mockResolvedValue(unpublished)

    const result = await store.unpublish('1')

    expect(unpublishArticleMock).toHaveBeenCalledWith('1')
    expect(result.status).toBe('Draft')
    expect(store.articles[0]).toEqual(unpublished)
  })

  it('unpublish() sets error and rethrows on failure', async () => {
    unpublishArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(store.unpublish('1')).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
  })

  it('fetchArticles() passes page and pageSize through and updates total', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle()], total: 42 })

    const store = useKnowledgeBaseStore()
    await store.fetchArticles({ page: 3, pageSize: 10 })

    expect(listArticlesMock).toHaveBeenCalledWith({ page: 3, pageSize: 10 })
    expect(store.total).toBe(42)
  })
})

describe('knowledgeBase store — categories', () => {
  it('fetchCategories() populates categories on success', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory()])

    const store = useKnowledgeBaseStore()
    await store.fetchCategories()

    expect(store.categories).toEqual([makeCategory()])
    expect(store.categoriesLoading).toBe(false)
    expect(store.categoriesError).toBeNull()
  })

  it('fetchCategories() sets categoriesError and rethrows on failure', async () => {
    listCategoriesMock.mockRejectedValue(new Error('network down'))

    const store = useKnowledgeBaseStore()
    await expect(store.fetchCategories()).rejects.toThrow('network down')

    expect(store.categoriesError).toBe('errorLoad')
  })

  it('activeCategories only returns active categories', async () => {
    listCategoriesMock.mockResolvedValue([
      makeCategory({ id: 'a', name: 'Active One', isActive: true }),
      makeCategory({ id: 'b', name: 'Inactive One', isActive: false }),
    ])

    const store = useKnowledgeBaseStore()
    await store.fetchCategories()

    expect(store.activeCategories.map((c) => c.id)).toEqual(['a'])
  })

  it('createCategory() appends the created category', async () => {
    createCategoryMock.mockResolvedValue(makeCategory({ id: 'new-1', name: 'Billing' }))

    const store = useKnowledgeBaseStore()
    const result = await store.createCategory({ name: 'Billing', description: null })

    expect(result.name).toBe('Billing')
    expect(store.categories.map((c) => c.name)).toEqual(['Billing'])
  })

  it('createCategory() sets categoriesError and rethrows on failure (duplicate name)', async () => {
    createCategoryMock.mockRejectedValue(new ApiError(409, 'duplicate'))

    const store = useKnowledgeBaseStore()
    await expect(store.createCategory({ name: 'Billing', description: null })).rejects.toThrow('duplicate')

    expect(store.categoriesError).toBe('duplicate')
  })

  it('updateCategory() replaces the category in place', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', name: 'Original' })])
    const store = useKnowledgeBaseStore()
    await store.fetchCategories()

    updateCategoryMock.mockResolvedValue(makeCategory({ id: '1', name: 'Updated' }))
    await store.updateCategory('1', { name: 'Updated', description: null })

    expect(store.categories[0]?.name).toBe('Updated')
  })

  it('activateCategory() calls setCategoryStatus with isActive true', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', isActive: false })])
    const store = useKnowledgeBaseStore()
    await store.fetchCategories()

    setCategoryStatusMock.mockResolvedValue(makeCategory({ id: '1', isActive: true }))
    await store.activateCategory('1')

    expect(setCategoryStatusMock).toHaveBeenCalledWith('1', true)
    expect(store.categories[0]?.isActive).toBe(true)
  })

  it('deactivateCategory() calls setCategoryStatus with isActive false', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', isActive: true })])
    const store = useKnowledgeBaseStore()
    await store.fetchCategories()

    setCategoryStatusMock.mockResolvedValue(makeCategory({ id: '1', isActive: false }))
    await store.deactivateCategory('1')

    expect(setCategoryStatusMock).toHaveBeenCalledWith('1', false)
    expect(store.categories[0]?.isActive).toBe(false)
  })

  it('setArticleCategoryFilter() sets selectedCategoryId and refetches articles with categoryId', async () => {
    listArticlesMock.mockResolvedValue({ items: [], total: 0 })

    const store = useKnowledgeBaseStore()
    await store.setArticleCategoryFilter('cat-1')

    expect(store.selectedCategoryId).toBe('cat-1')
    expect(listArticlesMock).toHaveBeenCalledWith({ categoryId: 'cat-1' })
  })

  it('setArticleCategoryFilter(null) clears the filter and refetches without categoryId', async () => {
    listArticlesMock.mockResolvedValue({ items: [], total: 0 })

    const store = useKnowledgeBaseStore()
    await store.setArticleCategoryFilter(null)

    expect(store.selectedCategoryId).toBeNull()
    expect(listArticlesMock).toHaveBeenCalledWith({ categoryId: undefined })
  })
})
