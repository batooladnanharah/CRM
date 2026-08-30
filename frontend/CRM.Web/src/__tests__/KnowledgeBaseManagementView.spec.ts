import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import KnowledgeBaseManagementView from '@/modules/knowledgeBase/views/KnowledgeBaseManagementView.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type {
  createArticle,
  deleteArticle,
  getArticle,
  listArticles,
  listCategories,
  publishArticle,
  searchArticles,
  unpublishArticle,
  updateArticle,
} from '@/api/knowledgeBase'
import type { KnowledgeBaseArticle, KnowledgeBaseCategory } from '@/types/knowledgeBase'

const {
  listArticlesMock,
  createArticleMock,
  updateArticleMock,
  deleteArticleMock,
  getArticleMock,
  publishArticleMock,
  unpublishArticleMock,
  listCategoriesMock,
  searchArticlesMock,
  confirmMock,
} = vi.hoisted(() => ({
  listArticlesMock: vi.fn<typeof listArticles>(),
  createArticleMock: vi.fn<typeof createArticle>(),
  updateArticleMock: vi.fn<typeof updateArticle>(),
  deleteArticleMock: vi.fn<typeof deleteArticle>(),
  getArticleMock: vi.fn<typeof getArticle>(),
  publishArticleMock: vi.fn<typeof publishArticle>(),
  unpublishArticleMock: vi.fn<typeof unpublishArticle>(),
  listCategoriesMock: vi.fn<typeof listCategories>(),
  searchArticlesMock: vi.fn<typeof searchArticles>(),
  confirmMock: vi.fn<() => Promise<boolean>>(),
}))

vi.mock('@/api/knowledgeBase', () => ({
  listArticles: listArticlesMock,
  searchArticles: searchArticlesMock,
  getArticle: getArticleMock,
  getArticleBySlug: vi.fn<() => void>(),
  createArticle: createArticleMock,
  updateArticle: updateArticleMock,
  deleteArticle: deleteArticleMock,
  publishArticle: publishArticleMock,
  unpublishArticle: unpublishArticleMock,
  listCategories: listCategoriesMock,
  createCategory: vi.fn<() => void>(),
  updateCategory: vi.fn<() => void>(),
  setCategoryStatus: vi.fn<() => void>(),
}))

vi.mock('@/composables/useConfirm', () => ({ confirm: confirmMock }))

const ACTIVE_CATEGORY: KnowledgeBaseCategory = {
  id: 'cat-1',
  name: 'Account',
  description: null,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
}

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

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/knowledge-base', name: 'knowledge-base-management', component: KnowledgeBaseManagementView },
      { path: '/knowledge-base/:id', name: 'knowledge-base-edit', component: KnowledgeBaseManagementView },
    ],
  })
}

async function mountView(router: Router = makeRouter(), path = '/knowledge-base') {
  router.push(path)
  await router.isReady()

  return mount(KnowledgeBaseManagementView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  listArticlesMock.mockReset()
  createArticleMock.mockReset()
  updateArticleMock.mockReset()
  deleteArticleMock.mockReset()
  getArticleMock.mockReset()
  publishArticleMock.mockReset()
  unpublishArticleMock.mockReset()
  listCategoriesMock.mockReset()
  searchArticlesMock.mockReset()
  confirmMock.mockReset()
  listArticlesMock.mockResolvedValue({ items: [], total: 0 })
  listCategoriesMock.mockResolvedValue([ACTIVE_CATEGORY])
})

describe('KnowledgeBaseManagementView', () => {
  it('renders the list of articles', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ title: 'Billing FAQ' })], total: 1 })

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Billing FAQ')
  })

  it('renders the empty state when there are no articles', async () => {
    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No articles found.')
  })

  it('opens the create form and submits a new article', async () => {
    createArticleMock.mockResolvedValue(makeArticle({ id: 'new-1', title: 'New Article', slug: 'new-article' }))

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-title').setValue('New Article')
    await wrapper.find('#kb-category').setValue('cat-1')
    await wrapper.find('.kb-article-form').trigger('submit')
    await flushPromises()

    expect(createArticleMock).toHaveBeenCalledWith({
      title: 'New Article',
      slug: 'new-article',
      body: '',
      tags: [],
      status: 'Draft',
      categoryId: 'cat-1',
    })
    expect(wrapper.text()).toContain('New Article')
  })

  it('shows a validation error for an invalid slug', async () => {
    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-title').setValue('Some Title')
    await wrapper.find('#kb-slug').setValue('Not_Valid!')
    await wrapper.find('#kb-slug').trigger('blur')
    await flushPromises()

    expect(wrapper.text()).toContain('Slug must contain only lowercase letters, numbers, and hyphens.')
    expect(createArticleMock).not.toHaveBeenCalled()
  })

  it('shows a conflict error when the backend returns 409', async () => {
    createArticleMock.mockRejectedValue(new ApiError(409, 'slug_conflict'))

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-title').setValue('Duplicate Title')
    await wrapper.find('#kb-slug').setValue('duplicate-slug')
    await wrapper.find('#kb-category').setValue('cat-1')
    await wrapper.find('.kb-article-form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('This slug is already used by another article.')
  })

  it('deletes an article after confirmation', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1' })], total: 1 })
    deleteArticleMock.mockResolvedValue(undefined)
    confirmMock.mockResolvedValue(true)

    const wrapper = await mountView()
    await flushPromises()

    const deleteButton = wrapper.findAll('button').find((b) => b.text() === 'Delete')!
    await deleteButton.trigger('click')
    await flushPromises()

    expect(deleteArticleMock).toHaveBeenCalledWith('1')
  })

  it('opens the edit form pre-filled and submits the update', async () => {
    listArticlesMock.mockResolvedValue({
      items: [makeArticle({ id: '1', title: 'Original Title', slug: 'original-slug' })],
      total: 1,
    })
    updateArticleMock.mockResolvedValue(
      makeArticle({ id: '1', title: 'Updated Title', slug: 'original-slug' }),
    )

    const wrapper = await mountView()
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')

    const titleInput = wrapper.find('.kb-article-inline-form input[type="text"]')
    expect((titleInput.element as HTMLInputElement).value).toBe('Original Title')

    await titleInput.setValue('Updated Title')
    await wrapper.find('.kb-article-inline-form').trigger('submit')
    await flushPromises()

    expect(updateArticleMock).toHaveBeenCalledWith('1', {
      title: 'Updated Title',
      slug: 'original-slug',
      body: 'Steps to reset your password.',
      tags: ['account'],
      status: 'Draft',
      categoryId: 'cat-1',
    })
    expect(wrapper.text()).toContain('Updated Title')
  })

  it('opens directly into edit mode when mounted with a route id', async () => {
    getArticleMock.mockResolvedValue(makeArticle({ id: '1', title: 'Deep Linked Article' }))

    const wrapper = await mountView(makeRouter(), '/knowledge-base/1')
    await flushPromises()

    expect(getArticleMock).toHaveBeenCalledWith('1')
    const titleInput = wrapper.find('.kb-article-form input[type="text"]')
    expect((titleInput.element as HTMLInputElement).value).toBe('Deep Linked Article')
  })

  it('shows a Publish button for a Draft article and publishes it', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1', status: 'Draft' })], total: 1 })
    publishArticleMock.mockResolvedValue(makeArticle({ id: '1', status: 'Published', publishedAtUtc: '2026-01-02T00:00:00Z' }))

    const wrapper = await mountView()
    await flushPromises()

    const publishButton = wrapper.findAll('button').find((b) => b.text() === 'Publish')!
    expect(wrapper.findAll('button').find((b) => b.text() === 'Unpublish')).toBeUndefined()

    await publishButton.trigger('click')
    await flushPromises()

    expect(publishArticleMock).toHaveBeenCalledWith('1')
    expect(wrapper.findAll('button').find((b) => b.text() === 'Unpublish')).toBeTruthy()
  })

  it('shows an Unpublish button for a Published article and unpublishes it', async () => {
    listArticlesMock.mockResolvedValue({
      items: [makeArticle({ id: '1', status: 'Published', publishedAtUtc: '2026-01-01T00:00:00Z' })],
      total: 1,
    })
    unpublishArticleMock.mockResolvedValue(makeArticle({ id: '1', status: 'Draft' }))

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.findAll('button').find((b) => b.text() === 'Publish')).toBeUndefined()
    const unpublishButton = wrapper.findAll('button').find((b) => b.text() === 'Unpublish')!

    await unpublishButton.trigger('click')
    await flushPromises()

    expect(unpublishArticleMock).toHaveBeenCalledWith('1')
    expect(wrapper.findAll('button').find((b) => b.text() === 'Publish')).toBeTruthy()
  })

  it('shows an error state with a retry action when the list fails to load', async () => {
    listArticlesMock.mockReset()
    listArticlesMock.mockRejectedValueOnce(new Error('network down'))
    listArticlesMock.mockResolvedValueOnce({ items: [makeArticle({ title: 'Recovered Article' })], total: 1 })

    const wrapper = await mountView()
    await flushPromises()

    expect(listArticlesMock).toHaveBeenCalledTimes(1)

    const retryButton = wrapper.findAll('button').find((b) => /retry/i.test(b.text()))
    expect(retryButton).toBeTruthy()

    await retryButton!.trigger('click')
    await flushPromises()

    expect(listArticlesMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Recovered Article')
  })

  it('blocks submit when the title is whitespace-only', async () => {
    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-title').setValue('   ')
    await wrapper.find('#kb-slug').setValue('some-slug')
    await wrapper.find('#kb-category').setValue('cat-1')
    await flushPromises()

    const submitButton = wrapper.findAll('button').find((b) => b.text() === 'Save')!
    expect(submitButton.attributes('disabled')).toBeDefined()

    await wrapper.find('.kb-article-form').trigger('submit')
    await flushPromises()

    expect(createArticleMock).not.toHaveBeenCalled()
  })

  it('blocks submit when no category is selected', async () => {
    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-title').setValue('Some Title')
    await wrapper.find('#kb-slug').setValue('some-slug')
    await flushPromises()

    const submitButton = wrapper.findAll('button').find((b) => b.text() === 'Save')!
    expect(submitButton.attributes('disabled')).toBeDefined()
  })

  it('renders the category filter and refetches articles with categoryId on change', async () => {
    listCategoriesMock.mockResolvedValue([ACTIVE_CATEGORY, {
      id: 'cat-2', name: 'Shipping', description: null, isActive: true,
      createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
    }])

    const wrapper = await mountView()
    await flushPromises()

    const filterSelect = wrapper.find('#kb-category-filter')
    expect(filterSelect.exists()).toBe(true)

    await filterSelect.setValue('cat-2')
    await flushPromises()

    expect(listArticlesMock).toHaveBeenCalledWith({ categoryId: 'cat-2' })
  })

  it('article form category picker only lists active categories', async () => {
    listCategoriesMock.mockResolvedValue([
      ACTIVE_CATEGORY,
      {
        id: 'cat-inactive', name: 'Archived Topic', description: null, isActive: false,
        createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
      },
    ])

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await flushPromises()

    const options = wrapper.find('#kb-category').findAll('option')
    const optionTexts = options.map((o) => o.text())
    expect(optionTexts.some((text) => text.includes('Account'))).toBe(true)
    expect(optionTexts.some((text) => text.includes('Archived Topic'))).toBe(false)
  })

  it('edit form retains the article legacy inactive category as a labeled, disabled option', async () => {
    listArticlesMock.mockResolvedValue({
      items: [makeArticle({
        id: '1',
        categoryId: 'cat-inactive',
        category: { id: 'cat-inactive', name: 'Archived Topic', isActive: false },
      })],
      total: 1,
    })

    const wrapper = await mountView()
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')
    await flushPromises()

    const categorySelect = wrapper.find('.kb-article-inline-form select:first-of-type')
    expect(wrapper.text()).toContain('Archived Topic')
    expect((categorySelect.element as HTMLSelectElement).value).toBe('cat-inactive')

    const inactiveOption = wrapper.findAll('option').find((o) => o.text().includes('Archived Topic'))!
    expect(inactiveOption.attributes('disabled')).toBeDefined()
  })

  it('renders the search bar and submits on Enter (button), showing results and forwarding the category filter', async () => {
    listCategoriesMock.mockResolvedValue([ACTIVE_CATEGORY, {
      id: 'cat-2', name: 'Shipping', description: null, isActive: true,
      createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
    }])
    searchArticlesMock.mockResolvedValue({
      items: [{
        id: 'kb-1', title: 'Billing FAQ', category: { id: 'cat-2', name: 'Shipping' },
        excerpt: 'How to update your billing details.', status: 'Published',
      }],
      page: 1, pageSize: 10, totalCount: 1,
    })

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('#kb-search').setValue('billing')
    await wrapper.find('#kb-category-filter').setValue('cat-2')
    await wrapper.find('#kb-category-filter').element.dispatchEvent(new Event('change'))
    await flushPromises()
    await wrapper.find('form.toolbar').trigger('submit')
    await flushPromises()

    expect(searchArticlesMock).toHaveBeenCalledWith({ q: 'billing', categoryId: 'cat-2', page: 1, pageSize: 10 })
    expect(wrapper.text()).toContain('Billing FAQ')
    expect(wrapper.text()).toContain('How to update your billing details.')
  })

  it('shows a no-results message when the search returns zero items', async () => {
    searchArticlesMock.mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0 })

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('#kb-search').setValue('zzzznomatch')
    await wrapper.find('form.toolbar').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('No articles found. Try a different search term.')
  })

  it('does not render a search-results panel before a search has been submitted', async () => {
    const wrapper = await mountView()
    await flushPromises()

    expect(searchArticlesMock).not.toHaveBeenCalled()
    expect(wrapper.find('.kb-search-panel').exists()).toBe(false)
  })
})
