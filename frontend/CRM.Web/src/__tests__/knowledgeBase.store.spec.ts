import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useKnowledgeBaseStore } from '@/stores/knowledgeBase'
import { ApiError } from '@/api/http'
import type {
  createArticle,
  deleteArticle,
  getArticle,
  getArticleBySlug,
  listArticles,
  searchArticles,
  updateArticle,
} from '@/api/knowledgeBase'
import type { KnowledgeBaseArticle } from '@/types/knowledgeBase'

const {
  listArticlesMock,
  searchArticlesMock,
  getArticleMock,
  getArticleBySlugMock,
  createArticleMock,
  updateArticleMock,
  deleteArticleMock,
} = vi.hoisted(() => ({
  listArticlesMock: vi.fn<typeof listArticles>(),
  searchArticlesMock: vi.fn<typeof searchArticles>(),
  getArticleMock: vi.fn<typeof getArticle>(),
  getArticleBySlugMock: vi.fn<typeof getArticleBySlug>(),
  createArticleMock: vi.fn<typeof createArticle>(),
  updateArticleMock: vi.fn<typeof updateArticle>(),
  deleteArticleMock: vi.fn<typeof deleteArticle>(),
}))

vi.mock('@/api/knowledgeBase', () => ({
  listArticles: listArticlesMock,
  searchArticles: searchArticlesMock,
  getArticle: getArticleMock,
  getArticleBySlug: getArticleBySlugMock,
  createArticle: createArticleMock,
  updateArticle: updateArticleMock,
  deleteArticle: deleteArticleMock,
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
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    publishedAtUtc: null,
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
})

describe('knowledgeBase store', () => {
  it('has the expected initial state', () => {
    const store = useKnowledgeBaseStore()

    expect(store.articles).toEqual([])
    expect(store.currentArticle).toBeNull()
    expect(store.searchResults).toEqual([])
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

  it('search() populates searchResults without requiring debounce (that lives in the view)', async () => {
    const article = makeArticle({ title: 'Zzyzx Match' })
    searchArticlesMock.mockResolvedValue({ items: [article], total: 1 })

    const store = useKnowledgeBaseStore()
    const result = await store.search('zzyzx')

    expect(searchArticlesMock).toHaveBeenCalledWith({ q: 'zzyzx' })
    expect(store.searchResults).toEqual([article])
    expect(result.total).toBe(1)
  })

  it('search() sets error and rethrows on failure', async () => {
    searchArticlesMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(store.search('term')).rejects.toThrow('failed')

    expect(store.error).toBe('errorLoad')
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
      title: 'Apple Article', slug: 'apple-article', body: 'Body', tags: [], status: 'Draft',
    })

    expect(result.title).toBe('Apple Article')
    expect(store.articles.map((a) => a.title)).toEqual(['Apple Article'])
  })

  it('create() sets error and rethrows on failure', async () => {
    createArticleMock.mockRejectedValue(new ApiError(409, 'slug_conflict'))

    const store = useKnowledgeBaseStore()
    await expect(
      store.create({ title: 'Title', slug: 'slug', body: 'Body', tags: [], status: 'Draft' }),
    ).rejects.toThrow('slug_conflict')

    expect(store.error).toBe('slug_conflict')
  })

  it('update() replaces the article in place on success', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', title: 'Original' })], total: 1 })
    const store = useKnowledgeBaseStore()
    await store.fetchArticles()

    const updated = makeArticle({ id: '1', title: 'Updated' })
    updateArticleMock.mockResolvedValue(updated)
    await store.update('1', { title: 'Updated', slug: 'resetting-your-password', body: 'Body', tags: [], status: 'Draft' })

    expect(store.articles[0]).toEqual(updated)
  })

  it('update() sets error and rethrows on failure', async () => {
    updateArticleMock.mockRejectedValue(new Error('failed'))

    const store = useKnowledgeBaseStore()
    await expect(
      store.update('1', { title: 'Title', slug: 'slug', body: 'Body', tags: [], status: 'Draft' }),
    ).rejects.toThrow('failed')

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
})
