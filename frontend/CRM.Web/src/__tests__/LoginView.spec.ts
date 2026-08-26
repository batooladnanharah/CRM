import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import LoginView from '@/modules/auth/views/LoginView.vue'
import DashboardView from '@/modules/dashboard/views/DashboardView.vue'
import { i18n } from '@/i18n'
import { ApiError, NetworkError } from '@/api/http'
import type { loginRequest } from '@/api/auth'
import type { LoginResponse } from '@/types/auth'

const { loginRequestMock } = vi.hoisted(() => ({
  loginRequestMock: vi.fn<typeof loginRequest>(),
}))

vi.mock('@/api/auth', () => ({
  loginRequest: loginRequestMock,
}))

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
      { path: '/', name: 'dashboard', component: DashboardView, meta: { requiresAuth: true } },
    ],
  })
}

async function mountLoginView(router: Router) {
  router.push('/login')
  await router.isReady()

  return mount(LoginView, {
    global: {
      plugins: [router, i18n],
    },
  })
}

beforeEach(() => {
  setActivePinia(createPinia())
  loginRequestMock.mockReset()
  sessionStorage.clear()
})

describe('LoginView', () => {
  it('renders app name, email, password, and submit button', async () => {
    const wrapper = await mountLoginView(makeRouter())

    expect(wrapper.text()).toContain('CRM')
    expect(wrapper.find('#login-email').exists()).toBe(true)
    expect(wrapper.find('#login-password').exists()).toBe(true)
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
  })

  it('shows a required error for empty email and does not call the API', async () => {
    const wrapper = await mountLoginView(makeRouter())

    await wrapper.find('#login-password').setValue('Correct#Passw0rd!')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Email is required.')
    expect(loginRequestMock).not.toHaveBeenCalled()
  })

  it('disables the submit button and shows the submitting label while loading', async () => {
    let resolveLogin: (value: LoginResponse) => void
    loginRequestMock.mockReturnValue(
      new Promise((resolve) => {
        resolveLogin = resolve
      }),
    )

    const wrapper = await mountLoginView(makeRouter())
    await wrapper.find('#login-email').setValue('agent@crm.local')
    await wrapper.find('#login-password').setValue('Correct#Passw0rd!')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    const button = wrapper.find('button[type="submit"]')
    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toBe('Signing in…')

    resolveLogin!({ user: { id: '1', name: 'Agent', email: 'agent@crm.local', roles: [] }, token: 't' })
    await flushPromises()
  })

  it('shows the generic invalid-credentials message on a 401 and stays on /login', async () => {
    loginRequestMock.mockRejectedValue(new ApiError(401, 'Invalid email or password.'))

    const router = makeRouter()
    const wrapper = await mountLoginView(router)
    await wrapper.find('#login-email').setValue('agent@crm.local')
    await wrapper.find('#login-password').setValue('wrong-password')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe('Invalid email or password.')
    expect(router.currentRoute.value.path).toBe('/login')
  })

  it('shows a network error message on network failure', async () => {
    loginRequestMock.mockRejectedValue(new NetworkError())

    const wrapper = await mountLoginView(makeRouter())
    await wrapper.find('#login-email').setValue('agent@crm.local')
    await wrapper.find('#login-password').setValue('Correct#Passw0rd!')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toBe(
      'Cannot reach the server. Check your connection and try again.',
    )
  })

  it('redirects to / on successful login', async () => {
    loginRequestMock.mockResolvedValue({
      user: { id: '1', name: 'Agent', email: 'agent@crm.local', roles: ['agent'] },
      token: 'signed-jwt',
    })

    const router = makeRouter()
    const wrapper = await mountLoginView(router)
    await wrapper.find('#login-email').setValue('agent@crm.local')
    await wrapper.find('#login-password').setValue('Correct#Passw0rd!')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(router.currentRoute.value.path).toBe('/')
  })
})
