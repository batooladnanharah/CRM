import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import KnowledgeBaseSearchDialog from '@/modules/knowledgeBase/components/KnowledgeBaseSearchDialog.vue'
import { i18n } from '@/i18n'
import type { searchArticles } from '@/api/knowledgeBase'
import type { KnowledgeBaseArticle } from '@/types/knowledgeBase'

const { searchArticlesMock } = vi.hoisted(() => ({
  searchArticlesMock: vi.fn<typeof searchArticles>(),
}))

vi.mock('@/api/knowledgeBase', () => ({
  listArticles: vi.fn<() => void>(),
  searchArticles: searchArticlesMock,
  getArticle: vi.fn<() => void>(),
  getArticleBySlug: vi.fn<() => void>(),
  createArticle: vi.fn<() => void>(),
  updateArticle: vi.fn<() => void>(),
  deleteArticle: vi.fn<() => void>(),
}))

const pushMock = vi.fn<(location: unknown) => void>()

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: pushMock }),
}))

function makeArticle(overrides: Partial<KnowledgeBaseArticle> = {}): KnowledgeBaseArticle {
  return {
    id: '1',
    title: 'Resetting Your Password',
    slug: 'resetting-your-password',
    body: 'A '.repeat(100) + 'long body that should be truncated in the snippet preview.',
    tags: ['account', 'password'],
    status: 'Published',
    authorId: 'user-1',
    categoryId: 'cat-1',
    category: { id: 'cat-1', name: 'Account', isActive: true },
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    publishedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function mountDialog() {
  return mount(KnowledgeBaseSearchDialog, {
    global: { plugins: [i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  searchArticlesMock.mockReset()
  pushMock.mockReset()
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('KnowledgeBaseSearchDialog', () => {
  it('does not call search for a query shorter than 2 characters', async () => {
    const wrapper = mountDialog()

    await wrapper.find('input[type="search"]').setValue('a')
    await vi.advanceTimersByTimeAsync(500)

    expect(searchArticlesMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('No articles found.')
  })

  it('debounces input before calling search', async () => {
    searchArticlesMock.mockResolvedValue({ items: [makeArticle()], total: 1 })

    const wrapper = mountDialog()
    await wrapper.find('input[type="search"]').setValue('pass')

    expect(searchArticlesMock).not.toHaveBeenCalled()

    await vi.advanceTimersByTimeAsync(300)
    await flushPromises()

    expect(searchArticlesMock).toHaveBeenCalledTimes(1)
    expect(searchArticlesMock).toHaveBeenCalledWith({ q: 'pass' })
    expect(wrapper.text()).toContain('Resetting Your Password')
  })

  it('renders a title, snippet, and tags for each result', async () => {
    searchArticlesMock.mockResolvedValue({ items: [makeArticle()], total: 1 })

    const wrapper = mountDialog()
    await wrapper.find('input[type="search"]').setValue('password')
    await vi.advanceTimersByTimeAsync(300)
    await flushPromises()

    expect(wrapper.text()).toContain('Resetting Your Password')
    expect(wrapper.text()).toContain('account, password')
  })

  it('navigates to the article edit route when a result is clicked', async () => {
    searchArticlesMock.mockResolvedValue({ items: [makeArticle({ id: 'article-9' })], total: 1 })

    const wrapper = mountDialog()
    await wrapper.find('input[type="search"]').setValue('password')
    await vi.advanceTimersByTimeAsync(300)
    await flushPromises()

    await wrapper.find('.kb-search-result').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'knowledge-base-edit', params: { id: 'article-9' } })
  })

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountDialog()

    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })
})
