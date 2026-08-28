import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalKnowledgeBaseCategoryView from '@/modules/customerPortal/views/PortalKnowledgeBaseCategoryView.vue'
import { i18n } from '@/i18n'
import type { CustomerKnowledgeBaseArticleListItem } from '@/types/customerPortal'

function makeArticle(
  overrides: Partial<CustomerKnowledgeBaseArticleListItem> = {},
): CustomerKnowledgeBaseArticleListItem {
  return {
    id: '1',
    title: 'Resetting Your Password',
    slug: 'resetting-your-password',
    tags: ['account'],
    publishedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    portalCategoryArticles: [] as CustomerKnowledgeBaseArticleListItem[],
    portalCategoryArticlesLoading: false,
    portalCategoryArticlesError: null as string | null,
    fetchPortalCategoryArticles: vi.fn<() => Promise<void>>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/customerPortal', () => ({
  useCustomerPortalStore: () => fakeStore,
}))

const pushMock = vi.fn<(location: unknown) => void>()

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>()
  return { ...actual, useRouter: () => ({ push: pushMock }) }
})

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/portal/knowledge-base/category/:id', name: 'portal-knowledge-base-category', component: PortalKnowledgeBaseCategoryView },
      { path: '/portal/knowledge-base', name: 'portal-knowledge-base-list', component: { template: '<div />' } },
      { path: '/portal/knowledge-base/:id', name: 'portal-knowledge-base-article', component: { template: '<div />' } },
    ],
  })
}

async function mountView(router: Router = makeRouter(), path = '/portal/knowledge-base/category/cat-1') {
  router.push(path)
  await router.isReady()

  return mount(PortalKnowledgeBaseCategoryView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
  pushMock.mockReset()
})

describe('PortalKnowledgeBaseCategoryView', () => {
  it('fetches articles for the category id from the route', async () => {
    await mountView()

    expect(fakeStore.fetchPortalCategoryArticles).toHaveBeenCalledWith('cat-1')
  })

  it('renders articles from the store', async () => {
    fakeStore = makeFakeStore({ portalCategoryArticles: [makeArticle({ title: 'Billing FAQ' })] })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Billing FAQ')
  })

  it('shows the loading state', async () => {
    fakeStore = makeFakeStore({ portalCategoryArticlesLoading: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading help articles')
  })

  it('shows the empty state when there are no articles', async () => {
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('No articles are available yet.')
  })

  it('shows the error state with a retry action', async () => {
    fakeStore = makeFakeStore({ portalCategoryArticlesError: 'errorLoad' })
    const wrapper = await mountView()

    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')

    expect(fakeStore.fetchPortalCategoryArticles).toHaveBeenCalledTimes(2)
  })

  it('navigates to the article view when a row is clicked', async () => {
    fakeStore = makeFakeStore({ portalCategoryArticles: [makeArticle({ id: 'article-42' })] })
    const wrapper = await mountView()

    await wrapper.find('tbody tr').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'portal-knowledge-base-article', params: { id: 'article-42' } })
  })
})
