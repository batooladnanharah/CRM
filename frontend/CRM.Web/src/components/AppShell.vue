<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAiStore } from '@/stores/ai'
import { useLocale } from '@/composables/useLocale'
import KnowledgeBaseSearchDialog from '@/modules/knowledgeBase/components/KnowledgeBaseSearchDialog.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AiAvailabilityBadge from '@/components/ai/AiAvailabilityBadge.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const aiStore = useAiStore()
const { locale } = useLocale()
const sidebarOpen = ref(false)
const sidebarPinned = ref(false)
const sidebarHovered = ref(false)
const sidebarExpanded = computed(() => sidebarPinned.value || sidebarHovered.value)
const knowledgeBaseSearchOpen = ref(false)

onMounted(() => {
  void aiStore.loadStatus()
})

const routeTitleKeys: Record<string, string> = {
  dashboard: 'dashboard.title',
  customers: 'navigation.customers',
  'customer-create': 'customers.create.title',
  'customer-profile': 'customers.profile.title',
  'customer-edit': 'customers.edit.title',
  'tickets-list': 'navigation.tickets',
  'ticket-create': 'tickets.create.title',
  'ticket-details': 'tickets.details.title',
  'quick-replies-management': 'quickReplies.title',
  'communication-channels-management': 'communicationChannels.title',
  'sla-policies-management': 'sla.policies.title',
  reports: 'reports.title',
  'admin-users': 'security.users.title',
  'admin-audit-log': 'security.audit.title',
  'knowledge-base-management': 'knowledgeBase.title',
  'knowledge-base-edit': 'knowledgeBase.title',
  'portal-dashboard': 'portal.dashboard.title',
  'portal-tickets-list': 'portal.tickets.title',
  'portal-ticket-create': 'portal.ticket.submit.title',
  'portal-ticket-details': 'portal.ticket.details.title',
}

const navigation = computed(() => {
  if (authStore.isCustomer) {
    return [
      { label: t('portal.dashboard.title'), to: { name: 'portal-dashboard' }, icon: '⌂' },
      { label: t('portal.tickets.title'), to: { name: 'portal-tickets-list' }, icon: '☰' },
      { label: t('portal.dashboard.submitCta'), to: { name: 'portal-ticket-create' }, icon: '✎' },
    ]
  }

  const items = [
    { label: t('navigation.overview'), to: { name: 'dashboard' }, icon: '⌂' },
    { label: t('navigation.customers'), to: { name: 'customers' }, icon: '◉' },
    { label: t('navigation.tickets'), to: { name: 'tickets-list' }, icon: '☰' },
  ]
  if (authStore.isAdmin || authStore.isAgent) {
    items.push({
      label: t('knowledgeBase.nav'),
      to: { name: 'knowledge-base-management' },
      icon: '📖',
    })
  }
  if (authStore.isAdmin) {
    items.push({
      label: t('quickReplies.title'),
      to: { name: 'quick-replies-management' },
      icon: '✎',
    })
    items.push({
      label: t('communicationChannels.title'),
      to: { name: 'communication-channels-management' },
      icon: '✉',
    })
    items.push({
      label: t('sla.policies.title'),
      to: { name: 'sla-policies-management' },
      icon: '⏱',
    })
    items.push({
      label: t('nav.reports'),
      to: { name: 'reports' },
      icon: '📊',
    })
    items.push({
      label: t('security.nav.users'),
      to: { name: 'admin-users' },
      icon: '🛡',
    })
    items.push({
      label: t('security.nav.auditLog'),
      to: { name: 'admin-audit-log' },
      icon: '📜',
    })
  }
  return items
})

function isActive(name: string) {
  if (name === 'customers') {
    return route.name === name || String(route.name).startsWith('customer-')
  }
  if (name === 'tickets-list') {
    return route.name === name || String(route.name).startsWith('ticket-')
  }
  return route.name === name
}

function closeSidebar() {
  sidebarOpen.value = false
}

function toggleSidebarExpanded() {
  sidebarPinned.value = !sidebarPinned.value
}

function toggleLocale() {
  locale.value = locale.value === 'en' ? 'ar' : 'en'
}

function currentTitle() {
  const key = routeTitleKeys[String(route.name)] ?? 'navigation.workspace'
  return t(key)
}

async function onLogout() {
  await authStore.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="app-shell">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="closeSidebar"></div>
    <aside class="sidebar" :class="{ 'is-open': sidebarOpen, 'is-expanded': sidebarExpanded }" @mouseenter="sidebarHovered = true" @mouseleave="sidebarHovered = false">
      <AppButton
        class="sidebar-toggle"
        variant="ghost"
        size="sm"
        :aria-label="t('navigation.openMenu')"
        :aria-expanded="sidebarExpanded"
        @click="toggleSidebarExpanded"
      >☰</AppButton>
      <div class="brand-lockup">
        <span class="brand-mark">C</span>
        <div>
          <strong>{{ t('app.name') }}</strong>
          <small>{{ t('navigation.workspace') }}</small>
        </div>
      </div>

      <nav class="sidebar-nav" :aria-label="t('navigation.primary')">
        <span class="nav-label">{{ t('navigation.menu') }}</span>
        <router-link
          v-for="item in navigation"
          :key="item.label"
          :to="item.to"
          class="nav-link"
          :class="{ active: isActive(String(item.to.name)) }"
          @click="closeSidebar"
        >
          <span class="nav-icon" aria-hidden="true">{{ item.icon }}</span>
          <span class="nav-text">{{ item.label }}</span>
        </router-link>
      </nav>

      <div class="sidebar-footer">
        <span class="status-dot"></span>
        <span>{{ t('navigation.systemOnline') }}</span>
      </div>
    </aside>

    <div class="shell-content">
      <header class="topbar">
        <AppButton class="icon-button menu-toggle" variant="ghost" size="sm" :aria-label="t('navigation.openMenu')" @click="sidebarOpen = true">☰</AppButton>
        <div class="breadcrumbs"><span>{{ t('app.name') }}</span><span aria-hidden="true">/</span><strong>{{ currentTitle() }}</strong></div>
        <div class="topbar-actions">
          <div class="action-group">
            <AppButton
              v-if="authStore.isAdmin || authStore.isAgent"
              class="icon-button"
              variant="ghost"
              size="sm"
              :aria-label="t('knowledgeBase.searchPlaceholder')"
              :title="t('knowledgeBase.searchPlaceholder')"
              @click="knowledgeBaseSearchOpen = true"
            >🔍</AppButton>
            <AppButton class="language-button" variant="ghost" size="sm" @click="toggleLocale">{{ locale === 'en' ? 'عربي' : 'EN' }}</AppButton>
          </div>
          <div class="action-divider" aria-hidden="true"></div>
          <AiAvailabilityBadge />
          <div class="action-divider" aria-hidden="true"></div>
          <div class="profile-group">
            <div class="user-chip">
              <span class="avatar">{{ authStore.user?.name?.charAt(0).toUpperCase() }}</span>
              <span class="user-details"><strong>{{ authStore.user?.name }}</strong><small>{{ authStore.user?.roles[0] }}</small></span>
            </div>
            <AppButton class="icon-button" variant="ghost" size="sm" :aria-label="t('dashboard.logout')" :title="t('dashboard.logout')" @click="onLogout">↪</AppButton>
          </div>
        </div>
      </header>
      <main class="page-content"><router-view /></main>
    </div>

    <div v-if="knowledgeBaseSearchOpen" class="kb-search-overlay" @click.self="knowledgeBaseSearchOpen = false">
      <KnowledgeBaseSearchDialog @close="knowledgeBaseSearchOpen = false" />
    </div>
  </div>
</template>
