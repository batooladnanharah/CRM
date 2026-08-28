import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import KnowledgeBaseCategoriesView from '@/modules/knowledgeBase/views/KnowledgeBaseCategoriesView.vue'
import { i18n } from '@/i18n'
import { ApiError } from '@/api/http'
import type {
  createCategory,
  listCategories,
  setCategoryStatus,
  updateCategory,
} from '@/api/knowledgeBase'
import type { KnowledgeBaseCategory } from '@/types/knowledgeBase'

const {
  listCategoriesMock,
  createCategoryMock,
  updateCategoryMock,
  setCategoryStatusMock,
  confirmMock,
} = vi.hoisted(() => ({
  listCategoriesMock: vi.fn<typeof listCategories>(),
  createCategoryMock: vi.fn<typeof createCategory>(),
  updateCategoryMock: vi.fn<typeof updateCategory>(),
  setCategoryStatusMock: vi.fn<typeof setCategoryStatus>(),
  confirmMock: vi.fn<() => Promise<boolean>>(),
}))

vi.mock('@/api/knowledgeBase', () => ({
  listArticles: vi.fn<() => void>(),
  searchArticles: vi.fn<() => void>(),
  getArticle: vi.fn<() => void>(),
  getArticleBySlug: vi.fn<() => void>(),
  createArticle: vi.fn<() => void>(),
  updateArticle: vi.fn<() => void>(),
  deleteArticle: vi.fn<() => void>(),
  publishArticle: vi.fn<() => void>(),
  unpublishArticle: vi.fn<() => void>(),
  listCategories: listCategoriesMock,
  createCategory: createCategoryMock,
  updateCategory: updateCategoryMock,
  setCategoryStatus: setCategoryStatusMock,
}))

vi.mock('@/composables/useConfirm', () => ({ confirm: confirmMock }))

function makeCategory(overrides: Partial<KnowledgeBaseCategory> = {}): KnowledgeBaseCategory {
  return {
    id: '1',
    name: 'Billing',
    description: 'Billing related articles',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

async function mountView() {
  return mount(KnowledgeBaseCategoriesView, {
    global: { plugins: [i18n] },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  listCategoriesMock.mockReset()
  createCategoryMock.mockReset()
  updateCategoryMock.mockReset()
  setCategoryStatusMock.mockReset()
  confirmMock.mockReset()
  listCategoriesMock.mockResolvedValue([])
})

describe('KnowledgeBaseCategoriesView', () => {
  it('renders the list of categories', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ name: 'Billing FAQ' })])

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Billing FAQ')
  })

  it('renders the empty state when there are no categories', async () => {
    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No categories yet.')
  })

  it('shows the loading state while fetching', async () => {
    let resolveList!: (value: KnowledgeBaseCategory[]) => void
    listCategoriesMock.mockReturnValue(new Promise((resolve) => { resolveList = resolve }))

    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Loading categories')
    resolveList([])
    await flushPromises()
  })

  it('shows an error state with retry when the list fails to load', async () => {
    listCategoriesMock.mockReset()
    listCategoriesMock.mockRejectedValueOnce(new Error('network down'))
    listCategoriesMock.mockResolvedValueOnce([makeCategory({ name: 'Recovered' })])

    const wrapper = await mountView()
    await flushPromises()

    const retryButton = wrapper.findAll('button').find((b) => /retry/i.test(b.text()))
    expect(retryButton).toBeTruthy()

    await retryButton!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Recovered')
  })

  it('opens the create dialog and submits a new category', async () => {
    createCategoryMock.mockResolvedValue(makeCategory({ id: 'new-1', name: 'New Category' }))

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-category-name').setValue('New Category')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(createCategoryMock).toHaveBeenCalledWith({ name: 'New Category', description: null })
    expect(wrapper.text()).toContain('New Category')
  })

  it('shows a duplicate-name error when the backend returns 409', async () => {
    createCategoryMock.mockRejectedValue(new ApiError(409, 'duplicate'))

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('button').trigger('click')
    await wrapper.find('#kb-category-name').setValue('Billing')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Category name already exists.')
  })

  it('opens the edit dialog pre-filled and submits the update', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', name: 'Original Name' })])
    updateCategoryMock.mockResolvedValue(makeCategory({ id: '1', name: 'Updated Name' }))

    const wrapper = await mountView()
    await flushPromises()

    const editButton = wrapper.findAll('button').find((b) => b.text() === 'Edit')!
    await editButton.trigger('click')

    const nameInput = wrapper.find('#kb-category-name')
    expect((nameInput.element as HTMLInputElement).value).toBe('Original Name')

    await nameInput.setValue('Updated Name')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(updateCategoryMock).toHaveBeenCalledWith('1', { name: 'Updated Name', description: 'Billing related articles' })
    expect(wrapper.text()).toContain('Updated Name')
  })

  it('deactivates a category after confirmation', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', isActive: true })])
    setCategoryStatusMock.mockResolvedValue(makeCategory({ id: '1', isActive: false }))
    confirmMock.mockResolvedValue(true)

    const wrapper = await mountView()
    await flushPromises()

    const deactivateButton = wrapper.findAll('button').find((b) => b.text() === 'Deactivate')!
    await deactivateButton.trigger('click')
    await flushPromises()

    expect(setCategoryStatusMock).toHaveBeenCalledWith('1', false)
    expect(wrapper.text()).toContain('Activate')
  })

  it('does not deactivate when the confirmation is declined', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', isActive: true })])
    confirmMock.mockResolvedValue(false)

    const wrapper = await mountView()
    await flushPromises()

    const deactivateButton = wrapper.findAll('button').find((b) => b.text() === 'Deactivate')!
    await deactivateButton.trigger('click')
    await flushPromises()

    expect(setCategoryStatusMock).not.toHaveBeenCalled()
  })

  it('activates an inactive category without confirmation', async () => {
    listCategoriesMock.mockResolvedValue([makeCategory({ id: '1', isActive: false })])
    setCategoryStatusMock.mockResolvedValue(makeCategory({ id: '1', isActive: true }))

    const wrapper = await mountView()
    await flushPromises()

    const activateButton = wrapper.findAll('button').find((b) => b.text() === 'Activate')!
    await activateButton.trigger('click')
    await flushPromises()

    expect(confirmMock).not.toHaveBeenCalled()
    expect(setCategoryStatusMock).toHaveBeenCalledWith('1', true)
  })
})
