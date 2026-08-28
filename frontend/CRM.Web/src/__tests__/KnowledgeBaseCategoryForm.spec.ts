import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import KnowledgeBaseCategoryForm from '@/modules/knowledgeBase/components/KnowledgeBaseCategoryForm.vue'
import { i18n } from '@/i18n'

function mountForm(props: Record<string, unknown> = {}) {
  return mount(KnowledgeBaseCategoryForm, {
    global: { plugins: [i18n] },
    props,
  })
}

describe('KnowledgeBaseCategoryForm', () => {
  it('blocks submit and shows an error when the name is empty', async () => {
    const wrapper = mountForm()

    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Name is required.')
    expect(wrapper.emitted('save')).toBeUndefined()
  })

  it('blocks submit and shows an error when the name is whitespace-only', async () => {
    const wrapper = mountForm()

    await wrapper.find('#kb-category-name').setValue('   ')
    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Name is required.')
    expect(wrapper.emitted('save')).toBeUndefined()
  })

  it('blocks submit and shows an error when the name exceeds 120 characters', async () => {
    const wrapper = mountForm()

    await wrapper.find('#kb-category-name').setValue('a'.repeat(121))
    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Name must be 120 characters or fewer.')
    expect(wrapper.emitted('save')).toBeUndefined()
  })

  it('blocks submit and shows an error when the description exceeds 1000 characters', async () => {
    const wrapper = mountForm()

    await wrapper.find('#kb-category-name').setValue('Billing')
    await wrapper.find('#kb-category-description').setValue('a'.repeat(1001))
    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Description must be 1000 characters or fewer.')
    expect(wrapper.emitted('save')).toBeUndefined()
  })

  it('emits save with trimmed values', async () => {
    const wrapper = mountForm()

    await wrapper.find('#kb-category-name').setValue('  Billing  ')
    await wrapper.find('#kb-category-description').setValue('  Billing related articles  ')
    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('save')?.[0]?.[0]).toEqual({
      name: 'Billing',
      description: 'Billing related articles',
      isActive: true,
    })
  })

  it('emits save with a null description when left blank', async () => {
    const wrapper = mountForm()

    await wrapper.find('#kb-category-name').setValue('Billing')
    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('save')?.[0]?.[0]).toEqual({
      name: 'Billing',
      description: null,
      isActive: true,
    })
  })

  it('does not render the status field when creating (no category prop)', () => {
    const wrapper = mountForm()

    expect(wrapper.find('#kb-category-status').exists()).toBe(false)
  })

  it('renders and pre-fills the status field when editing', () => {
    const wrapper = mountForm({
      category: {
        id: '1',
        name: 'Billing',
        description: 'desc',
        isActive: false,
        createdAt: '2026-01-01T00:00:00Z',
        updatedAt: '2026-01-01T00:00:00Z',
      },
    })

    expect(wrapper.find('#kb-category-status').exists()).toBe(true)
    expect((wrapper.find('#kb-category-name').element as HTMLInputElement).value).toBe('Billing')
  })

  it('emits cancel', async () => {
    const wrapper = mountForm()

    await wrapper.findAll('button').find((b) => b.text() === 'Cancel')!.trigger('click')

    expect(wrapper.emitted('cancel')).toBeTruthy()
  })
})
