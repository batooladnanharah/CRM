<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useNotificationsStore } from '@/stores/notifications'
import { useRelativeTime } from '@/composables/useRelativeTime'
import AppButton from '@/components/ui/AppButton.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import type { AppNotification } from '@/types/notifications'

const { t } = useI18n()
const router = useRouter()
const store = useNotificationsStore()
const { formatRelativeTime } = useRelativeTime()

const open = ref(false)
const rootRef = ref<HTMLElement | null>(null)

onMounted(() => {
  void store.fetch()
  store.startPolling()
  document.addEventListener('click', onDocumentClick)
})

onUnmounted(() => {
  store.stopPolling()
  document.removeEventListener('click', onDocumentClick)
})

function onDocumentClick(event: MouseEvent) {
  if (open.value && rootRef.value && !rootRef.value.contains(event.target as Node)) {
    open.value = false
  }
}

function togglePanel() {
  open.value = !open.value
  if (open.value) {
    void store.fetch()
  }
}

async function onItemClick(item: AppNotification) {
  await store.markRead(item.id)
  if (item.ticketId) {
    open.value = false
    void router.push(`/tickets/${item.ticketId}`)
  }
}

async function onMarkAllRead() {
  await store.markAllRead()
}
</script>

<template>
  <div ref="rootRef" class="notification-bell">
    <AppButton
      class="icon-button"
      variant="ghost"
      size="sm"
      :aria-label="t('notificationCenter.bellLabel')"
      :title="t('notificationCenter.bellLabel')"
      @click="togglePanel"
    >
      🔔
      <span v-if="store.unreadCount > 0" class="unread-badge">{{ store.unreadCount > 99 ? '99+' : store.unreadCount }}</span>
    </AppButton>

    <div v-if="open" class="notification-panel surface">
      <div class="panel-header">
        <strong>{{ t('notificationCenter.title') }}</strong>
        <AppButton variant="ghost" size="sm" type="button" @click="onMarkAllRead">
          {{ t('notificationCenter.markAllRead') }}
        </AppButton>
      </div>

      <LoadingState v-if="store.loading" :label="t('notificationCenter.loading')" />
      <AppAlert v-else-if="store.error" tone="danger" role="alert">{{ t('notificationCenter.error') }}</AppAlert>
      <EmptyState v-else-if="store.items.length === 0" :description="t('notificationCenter.empty')" />

      <ul v-else class="notification-list">
        <li
          v-for="item in store.items"
          :key="item.id"
          class="notification-item"
          :class="{ unread: !item.isRead }"
          @click="onItemClick(item)"
        >
          <span class="status-dot" :class="{ filled: !item.isRead }" aria-hidden="true"></span>
          <div class="notification-body">
            <strong>{{ item.title }}</strong>
            <p>{{ item.message }}</p>
            <small>{{ formatRelativeTime(item.createdAt) }}</small>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.notification-bell {
  position: relative;
}

.unread-badge {
  position: absolute;
  top: -0.25rem;
  right: -0.25rem;
  background: var(--color-danger, #dc2626);
  color: #fff;
  border-radius: 999px;
  font-size: 0.65rem;
  padding: 0 0.35rem;
  min-width: 1rem;
  line-height: 1.3rem;
  text-align: center;
}

.notification-panel {
  position: absolute;
  top: calc(100% + var(--space-2, 0.5rem));
  right: 0;
  width: 22rem;
  max-width: 90vw;
  max-height: 28rem;
  overflow-y: auto;
  z-index: 40;
  padding: var(--space-3, 0.75rem);
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-2, 0.5rem);
}

.notification-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2, 0.5rem);
}

.notification-item {
  display: flex;
  gap: var(--space-2, 0.5rem);
  cursor: pointer;
  padding: var(--space-2, 0.5rem);
  border-radius: var(--radius-md, 0.5rem);
}

.notification-item:hover {
  background: var(--color-surface-hover, rgba(0, 0, 0, 0.04));
}

.notification-item.unread strong {
  font-weight: 700;
}

.status-dot {
  flex: none;
  width: 0.6rem;
  height: 0.6rem;
  margin-top: 0.35rem;
  border-radius: 999px;
  border: 1.5px solid currentColor;
}

.status-dot.filled {
  background: currentColor;
}

.notification-body p {
  margin: 0.15rem 0;
  font-size: 0.85rem;
}

.notification-body small {
  color: var(--color-text-muted, #6b7280);
}
</style>
