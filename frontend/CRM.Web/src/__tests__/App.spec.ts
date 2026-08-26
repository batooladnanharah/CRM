import { describe, it, expect } from 'vitest'

import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import App from '../App.vue'
import LoginView from '@/modules/auth/views/LoginView.vue'
import DashboardView from '@/modules/dashboard/views/DashboardView.vue'
import { i18n } from '../i18n'

function makeTestRouter() {
  return createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
      { path: '/', name: 'dashboard', component: DashboardView, meta: { requiresAuth: true } },
    ],
  })
}

describe('App', () => {
  it('renders the routed view via router-view', async () => {
    const router = makeTestRouter()
    router.push('/login')
    await router.isReady()

    const wrapper = mount(App, {
      global: {
        plugins: [createPinia(), router, i18n],
      },
    })

    expect(wrapper.find('#login-email').exists()).toBe(true)
  })
})
