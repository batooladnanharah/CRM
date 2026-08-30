import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import KnowledgeBaseSearchDialog from '@/modules/knowledgeBase/components/KnowledgeBaseSearchDialog.vue'
import { i18n } from '@/i18n'
import type { searchArticles } from '@/api/knowledgeBase'
import type { KnowledgeBaseSearchItem } from '@/types/knowledgeBase'

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

function makeSearchItem(overrides: Partial<KnowledgeBaseSearchItem> = {}): KnowledgeBaseSearchItem {
  return {
    id: '1',
    title: 'Resetting Your Password',
    category: { id: 'cat-1', name: 'Account' },
    excerpt: 'Steps to reset your password using the account recovery flow.',
    status: 'Published',
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
})

async function submitQuery(wrapper: ReturnType<typeof mountDialog>, value: string) {
  await wrapper.find('input[type="search"]').setValue(value)
  await wrapper.find('form').trigger('submit')
  await flushPromises()
}

describe('KnowledgeBaseSearchDialog', () => {
  it('does not call the search API for a query shorter than 2 characters', async () => {
    const wrapper = mountDialog()

    await submitQuery(wrapper, 'a')

    expect(searchArticlesMock).not.toHaveBeenCalled()
  })

  it('calls the search API only on submit (not on every keystroke)', async () => {
    searchArticlesMock.mockResolvedValue({
      items: [makeSearchItem()], page: 1, pageSize: 10, totalCount: 1,
    })

    const wrapper = mountDialog()
    await wrapper.find('input[type="search"]').setValue('pass')

    expect(searchArticlesMock).not.toHaveBeenCalled()

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(searchArticlesMock).toHaveBeenCalledTimes(1)
    expect(searchArticlesMock).toHaveBeenCalledWith({ q: 'pass' })
    expect(wrapper.text()).toContain('Resetting Your Password')
  })

  it('renders a title, excerpt, and category for each result with no client-side filtering', async () => {
    searchArticlesMock.mockResolvedValue({
      items: [makeSearchItem()], page: 1, pageSize: 10, totalCount: 1,
    })

    const wrapper = mountDialog()
    await submitQuery(wrapper, 'password')

    expect(wrapper.text()).toContain('Resetting Your Password')
    expect(wrapper.text()).toContain('Account')
    expect(wrapper.text()).toContain('Steps to reset your password using the account recovery flow.')
  })

  it('emits select-article and navigates to the article edit route when a result is clicked', async () => {
    searchArticlesMock.mockResolvedValue({
      items: [makeSearchItem({ id: 'article-9' })], page: 1, pageSize: 10, totalCount: 1,
    })

    const wrapper = mountDialog()
    await submitQuery(wrapper, 'password')

    await wrapper.find('.kb-search-result').trigger('click')

    expect(wrapper.emitted('select-article')).toEqual([['article-9']])
    expect(pushMock).toHaveBeenCalledWith({ name: 'knowledge-base-edit', params: { id: 'article-9' } })
  })

  it('renders a no-results message when the search returns nothing', async () => {
    searchArticlesMock.mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0 })

    const wrapper = mountDialog()
    await submitQuery(wrapper, 'zzzznomatch')

    expect(wrapper.text()).toContain('No articles found. Try a different search term.')
  })

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountDialog()

    const closeButton = wrapper.findAll('button').find((b) => b.text() === 'Close')!
    await closeButton.trigger('click')

    expect(wrapper.emitted('close')).toHaveLength(1)
  })
})
