import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { i18n } from '@/i18n'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppCard from '@/components/ui/AppCard.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import AppDialog from '@/components/ui/AppDialog.vue'
import AppTable from '@/components/ui/AppTable.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'

const global = { plugins: [i18n] }

describe('design system primitives', () => {
  it('AppButton renders with its token class and slot content', () => {
    const wrapper = mount(AppButton, { global, slots: { default: 'Save' } })
    expect(wrapper.classes()).toContain('ui-button')
    expect(wrapper.text()).toBe('Save')
  })

  it('AppInput renders a labeled field', () => {
    const wrapper = mount(AppInput, { global, props: { label: 'Name', modelValue: '' } })
    expect(wrapper.find('.ui-input').exists()).toBe(true)
    expect(wrapper.find('label').text()).toBe('Name')
  })

  it('AppCard renders header/body/footer slots', () => {
    const wrapper = mount(AppCard, {
      global,
      props: { title: 'Details' },
      slots: { default: 'Body', footer: 'Footer' },
    })
    expect(wrapper.classes()).toContain('ui-card')
    expect(wrapper.text()).toContain('Details')
    expect(wrapper.text()).toContain('Body')
    expect(wrapper.text()).toContain('Footer')
  })

  it('AppBadge renders its tone class', () => {
    const wrapper = mount(AppBadge, { global, props: { tone: 'success' }, slots: { default: 'Active' } })
    expect(wrapper.classes()).toContain('ui-badge--success')
  })

  it('AppAlert renders and can be dismissed', async () => {
    const wrapper = mount(AppAlert, { global, props: { dismissible: true }, slots: { default: 'Heads up' } })
    expect(wrapper.classes()).toContain('ui-alert')
    await wrapper.find('.ui-alert__dismiss').trigger('click')
    expect(wrapper.emitted('dismiss')).toBeTruthy()
  })

  it('AppPagination disables previous on the first page and emits update:page', async () => {
    const wrapper = mount(AppPagination, { global, props: { page: 1, pageSize: 10, totalCount: 25 } })
    expect(wrapper.classes()).toContain('ui-pagination')
    const buttons = wrapper.findAll('button')
    expect(buttons[0]!.attributes('disabled')).toBeDefined()
    await buttons[1]!.trigger('click')
    expect(wrapper.emitted('update:page')?.[0]).toEqual([2])
  })

  it('AppDialog renders as a modal and emits close', async () => {
    const wrapper = mount(AppDialog, { global, props: { title: 'Confirm' }, attachTo: document.body })
    expect(wrapper.find('.ui-dialog').attributes('role')).toBe('dialog')
    await wrapper.find('.ui-dialog__close').trigger('click')
    expect(wrapper.emitted('close')).toBeTruthy()
    wrapper.unmount()
  })

  it('AppTable renders a desktop table and a mobile card list from the same items', () => {
    const wrapper = mount(AppTable, {
      global,
      props: { items: [{ id: 1 }] },
      slots: {
        head: '<th>Name</th>',
        row: '<td>Row</td>',
        'row-card': '<div>Card</div>',
      },
    })
    expect(wrapper.classes()).toContain('ui-table')
    expect(wrapper.find('.ui-table__table').exists()).toBe(true)
    expect(wrapper.find('.ui-table__cards').exists()).toBe(true)
  })

  it('LoadingState renders the shared loading copy', () => {
    const wrapper = mount(LoadingState, { global })
    expect(wrapper.classes()).toContain('ui-loading-state')
    expect(wrapper.text()).toContain('Loading')
  })

  it('EmptyState renders the shared empty copy', () => {
    const wrapper = mount(EmptyState, { global })
    expect(wrapper.classes()).toContain('ui-empty-state')
  })

  it('ErrorState renders a retry button that emits retry', async () => {
    const wrapper = mount(ErrorState, { global })
    expect(wrapper.classes()).toContain('ui-error-state')
    await wrapper.find('.ui-error-state__retry').trigger('click')
    expect(wrapper.emitted('retry')).toBeTruthy()
  })
})
