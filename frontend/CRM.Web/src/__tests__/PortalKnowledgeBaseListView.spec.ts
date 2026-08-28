import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import PortalKnowledgeBaseListView from '@/modules/customerPortal/views/PortalKnowledgeBaseListView.vue'
import { i18n } from '@/i18n'
import type { CustomerKnowledgeBaseCategorySummary } from '@/types/customerPortal'

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

function makeFakeStore(overrides: Record<string, unknown> = {}) {
  return reactive({
    portalCategories: [] as CustomerKnowledgeBaseCategorySummary[],
    portalCategoriesLoading: false,
    portalCategoriesError: null as string | null,
    fetchPortalCategories: vi.fn<() => Promise<void>>(),
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
})
