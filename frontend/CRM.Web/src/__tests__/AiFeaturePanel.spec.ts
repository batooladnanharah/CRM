import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AiFeaturePanel from '@/components/ai/AiFeaturePanel.vue'
import { i18n } from '@/i18n'

function mountPanel(props: Partial<InstanceType<typeof AiFeaturePanel>['$props']> = {}) {
  return mount(AiFeaturePanel, {
    props: { state: 'idle', ...props },
    global: { plugins: [i18n] },
    slots: { default: 'AI generated content' },
  })
}

describe('AiFeaturePanel', () => {
  it('unavailable state shows the unavailable alert', () => {
    const wrapper = mountPanel({ state: 'unavailable' })

    expect(wrapper.text()).toContain('AI assistance is not configured')
  })

  it('idle state renders a generate button and emits @generate on click', async () => {
    const wrapper = mountPanel({ state: 'idle', generateLabel: 'Summarise' })

    expect(wrapper.text()).toContain('Summarise')
    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('generate')).toBeTruthy()
  })

  it('loading state renders a loading indicator and a cancel button that emits @cancel', async () => {
    const wrapper = mountPanel({ state: 'loading' })

    expect(wrapper.text()).toContain('Generating')
    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('cancel')).toBeTruthy()
  })

  it('error state renders the localized error and a try-again button that emits @retry', async () => {
    const wrapper = mountPanel({ state: 'error' })

    expect(wrapper.text()).toContain('AI summary could not be generated')
    const retryButton = wrapper.findAll('button').find((b) => b.text().includes('Try again'))!
    await retryButton.trigger('click')

    expect(wrapper.emitted('retry')).toBeTruthy()
  })

  it('success state renders the default slot content', () => {
    const wrapper = mountPanel({ state: 'success' })

    expect(wrapper.text()).toContain('AI generated content')
  })

  it('shows the development disclaimer in success state when provider is Development', () => {
    const wrapper = mountPanel({ state: 'success', provider: 'Development' })

    expect(wrapper.text()).toContain('Development / mock response')
  })

  it('does not show the development disclaimer when provider is not Development', () => {
    const wrapper = mountPanel({ state: 'success', provider: 'OpenAI' })

    expect(wrapper.text()).not.toContain('Development / mock response')
  })
})
