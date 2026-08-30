import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalKnowledgeBaseListView from '@/modules/customerPortal/views/PortalKnowledgeBaseListView.vue'
import { i18n } from '@/i18n'
import type { CustomerKnowledgeBaseCategorySummary } from '@/types/customerPortal'
import type { KnowledgeBaseSearchItem } from '@/types/knowledgeBase'

function makeCategory(
  overrides: Partial<CustomerKnowledgeBaseCategorySummary> = {},
): CustomerKnowledgeBaseCategorySummary {
  return {
    id: '1',
    name: 'Billing',
    description: 'Billing related articles',
    articleCount: 3,
    ...overrides,
  }
}

function makeSearchItem(overrides: Partial<KnowledgeBaseSearchItem> = {}): KnowledgeBaseSearchItem {
  return {
    id: 'kb-1',
    title: 'Resetting Your Password',
    category: { id: 'cat-1', name: 'Account' },
    excerpt: 'Steps to reset your password.',
    status: null,
    ...overrides,
  }
}

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    portalCategories: [] as CustomerKnowledgeBaseCategorySummary[],
    portalCategoriesLoading: false,
    portalCategoriesError: null as string | null,
    fetchPortalCategories: vi.fn<() => Promise<void>>(),
    knowledgeBaseSearch: {
      query: '',
      categoryId: null as string | null,
      page: 1,
      pageSize: 10,
      items: [] as KnowledgeBaseSearchItem[],
      totalCount: 0,
      loading: false,
      error: null as string | null,
      lastQuery: '',
    },
    runKnowledgeBaseSearch: vi.fn<(options: { query: string; categoryId?: string | null; page?: number }) => Promise<void>>(),
    setKnowledgeBaseSearchPage: vi.fn<(page: number) => Promise<void>>(),
    resetKnowledgeBaseSearch: vi.fn<() => void>(),
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
      { path: '/portal/knowledge-base', name: 'portal-knowledge-base-list', component: PortalKnowledgeBaseListView },
      { path: '/portal/knowledge-base/category/:id', name: 'portal-knowledge-base-category', component: { template: '<div />' } },
      { path: '/portal/knowledge-base/:id', name: 'portal-knowledge-base-article', component: { template: '<div />' } },
      { path: '/portal/tickets/new', name: 'portal-ticket-create', component: { template: '<div />' } },
    ],
  })
}

async function mountView(router: Router = makeRouter()) {
  router.push('/portal/knowledge-base')
  await router.isReady()

  return mount(PortalKnowledgeBaseListView, {
    global: { plugins: [router, i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  fakeStore = makeFakeStore()
  pushMock.mockReset()
})

describe('PortalKnowledgeBaseListView', () => {
  it('calls fetchPortalCategories on mount', async () => {
    await mountView()

    expect(fakeStore.fetchPortalCategories).toHaveBeenCalledOnce()
  })

  it('renders category cards with article counts', async () => {
    fakeStore = makeFakeStore({ portalCategories: [makeCategory({ name: 'Billing FAQ', articleCount: 5 })] })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Billing FAQ')
    expect(wrapper.text()).toContain('5')
  })

  it('shows the loading state', async () => {
    fakeStore = makeFakeStore({ portalCategoriesLoading: true })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading help articles')
  })

  it('hides categories and shows the empty state when the response is empty', async () => {
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('No help categories are available.')
    expect(wrapper.findAll('.category-card').length).toBe(0)
  })

  it('shows the error state with a retry action', async () => {
    fakeStore = makeFakeStore({ portalCategoriesError: 'errorLoad' })
    const wrapper = await mountView()

    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Retry')!
    await retryButton.trigger('click')

    expect(fakeStore.fetchPortalCategories).toHaveBeenCalledTimes(2)
  })

  it('navigates to the category view when a card is clicked', async () => {
    fakeStore = makeFakeStore({ portalCategories: [makeCategory({ id: 'cat-42' })] })
    const wrapper = await mountView()

    await wrapper.find('.category-card').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'portal-knowledge-base-category', params: { id: 'cat-42' } })
  })

  it('renders a search input and calls runKnowledgeBaseSearch on submit without includeDrafts', async () => {
    fakeStore = makeFakeStore({
      runKnowledgeBaseSearch: vi.fn(async (options: { query: string }) => {
        fakeStore.knowledgeBaseSearch.lastQuery = options.query
        fakeStore.knowledgeBaseSearch.items = [makeSearchItem()]
        fakeStore.knowledgeBaseSearch.totalCount = 1
      }),
    })
    const wrapper = await mountView()

    await wrapper.find('.portal-search-form input').setValue('password')
    await wrapper.find('.portal-search-form').trigger('submit')

    expect(fakeStore.runKnowledgeBaseSearch).toHaveBeenCalledWith({ query: 'password', page: 1 })
    const callArgs = (fakeStore.runKnowledgeBaseSearch as ReturnType<typeof vi.fn>).mock.calls[0]?.[0] as
      { query: string; includeDrafts?: boolean }
    expect(callArgs.includeDrafts).toBeUndefined()
    expect(wrapper.text()).toContain('Resetting Your Password')
  })

  it('accepts Arabic input in the search field and forwards it verbatim', async () => {
    fakeStore = makeFakeStore({
      runKnowledgeBaseSearch: vi.fn(async (options: { query: string }) => {
        fakeStore.knowledgeBaseSearch.lastQuery = options.query
      }),
    })
    const wrapper = await mountView()

    const arabicQuery = 'كلمة المرور'
    await wrapper.find('.portal-search-form input').setValue(arabicQuery)
    await wrapper.find('.portal-search-form').trigger('submit')

    expect(fakeStore.runKnowledgeBaseSearch).toHaveBeenCalledWith({ query: arabicQuery, page: 1 })
  })

  it('shows a "Contact Support" link on no results that routes to the portal ticket-create route', async () => {
    fakeStore = makeFakeStore({
      knowledgeBaseSearch: {
        query: 'nomatch', categoryId: null, page: 1, pageSize: 10, items: [], totalCount: 0,
        loading: false, error: null, lastQuery: 'nomatch',
      },
    })
    const wrapper = await mountView()
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('No articles found. Try a different search term.')
    const contactButton = wrapper.findAll('button').find((b) => b.text() === 'Contact Support')!
    await contactButton.trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'portal-ticket-create' })
  })

  it('does not show a status badge on portal search results', async () => {
    fakeStore = makeFakeStore({
      knowledgeBaseSearch: {
        query: 'password', categoryId: null, page: 1, pageSize: 10, items: [makeSearchItem()], totalCount: 1,
        loading: false, error: null, lastQuery: 'password',
      },
    })
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Resetting Your Password')
    expect(wrapper.find('.ui-badge').exists()).toBe(false)
  })
})
