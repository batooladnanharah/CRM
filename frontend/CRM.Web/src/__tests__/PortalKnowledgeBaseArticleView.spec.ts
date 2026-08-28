import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalKnowledgeBaseArticleView from '@/modules/customerPortal/views/PortalKnowledgeBaseArticleView.vue'
import { i18n } from '@/i18n'
import type { CustomerKnowledgeBaseArticleDetails } from '@/types/customerPortal'

function makeArticleDetails(
  overrides: Partial<CustomerKnowledgeBaseArticleDetails> = {},
): CustomerKnowledgeBaseArticleDetails {
  return {
    id: '1',
    title: 'Resetting Your Password',
    slug: 'resetting-your-password',
    body: 'Step 1.\nStep 2.',
    tags: ['account'],
    publishedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    currentArticle: null as CustomerKnowledgeBaseArticleDetails | null,
    articleLoading: false,
    articleError: null as string | null,
    fetchArticle: vi.fn<() => Promise<void>>(),
    ...overrides,
  })
}

let fakeStore = makeFakeStore()

vi.mock('@/stores/customerPortal', () => ({
  useCustomerPortalStore: () => fakeStore,
}))

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/portal/knowledge-base', name: 'portal-knowledge-base-list', component: { template: '<div />' } },
      { path: '/portal/knowledge-base/:id', name: 'portal-knowledge-base-article', component: PortalKnowledgeBaseArticleView },
    ],
  })
}

async function mountView(path = '/portal/knowledge-base/1', router: Router = makeRouter()) {
  router.push(path)
  await router.isReady()

  return mount(PortalKnowledgeBaseArticleView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
})

describe('PortalKnowledgeBaseArticleView', () => {
  it('calls fetchArticle on mount with the route id', async () => {
    await mountView('/portal/knowledge-base/article-42')

    expect(fakeStore.fetchArticle).toHaveBeenCalledWith('article-42')
  })

  it('shows the loading state', async () => {
    fakeStore = makeFakeStore({ articleLoading: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading help articles')
  })

  it('shows the article title, published date, and content once loaded', async () => {
    fakeStore = makeFakeStore({ currentArticle: makeArticleDetails() })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Resetting Your Password')
    expect(wrapper.text()).toContain('Step 1.')
  })

  // A draft/archived/missing article all surface as a 404 from the API —
  // the store sets articleError without ever exposing a draft indicator, so
  // this view must show the same not-found copy for all three cases.
  it('shows the not-found state (no draft indicator) when the API returns 404', async () => {
    fakeStore = makeFakeStore({ articleError: 'errorLoad', currentArticle: null })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain("We couldn't find that article.")
    expect(wrapper.text()).not.toMatch(/draft/i)
    expect(wrapper.text()).not.toMatch(/archived/i)
  })
})
