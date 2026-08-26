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
  updateArticle,
} from '@/api/knowledgeBase'
import type { KnowledgeBaseArticle } from '@/types/knowledgeBase'

const { listArticlesMock, createArticleMock, updateArticleMock, deleteArticleMock, getArticleMock } =
  vi.hoisted(() => ({
    listArticlesMock: vi.fn<typeof listArticles>(),
    createArticleMock: vi.fn<typeof createArticle>(),
    updateArticleMock: vi.fn<typeof updateArticle>(),
    deleteArticleMock: vi.fn<typeof deleteArticle>(),
    getArticleMock: vi.fn<typeof getArticle>(),
  }))

vi.mock('@/api/knowledgeBase', () => ({
  listArticles: listArticlesMock,
  searchArticles: vi.fn<() => void>(),
  getArticle: getArticleMock,
  getArticleBySlug: vi.fn<() => void>(),
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
  listArticlesMock.mockResolvedValue({ items: [], total: 0 })
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
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createArticleMock).toHaveBeenCalledWith({
      title: 'New Article',
      slug: 'new-article',
      body: '',
      tags: [],
      status: 'Draft',
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
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('This slug is already used by another article.')
  })

  it('deletes an article after confirmation', async () => {
    listArticlesMock.mockResolvedValue({ items: [makeArticle({ id: '1' })], total: 1 })
    deleteArticleMock.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

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
})
