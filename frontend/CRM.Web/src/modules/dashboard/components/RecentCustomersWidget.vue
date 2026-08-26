<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useRelativeTime } from '@/composables/useRelativeTime'
import type { RecentCustomerEntry } from '@/types/dashboard'

defineProps<{ customers: RecentCustomerEntry[]; loading: boolean }>()

const { t } = useI18n()
const router = useRouter()
const { formatRelativeTime } = useRelativeTime()

function onItemClick(id: string) {
  router.push({ name: 'customer-profile', params: { id } })
}
</script>

<template>
  <section class="surface widget">
    <h3>{{ t('dashboard.widgets.recentCustomers') }}</h3>

    <ul v-if="loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="customers.length === 0" class="empty-state">
      <p>{{ t('dashboard.empty.recentCustomers') }}</p>
    </div>

    <ul v-else class="customers-list">
      <li
        v-for="customer in customers"
        :key="customer.id"
        class="customer-item"
        @click="onItemClick(customer.id)"
      >
        <span class="customer-name truncate" :title="customer.name">{{ customer.name }}</span>
        <span class="customer-time">{{ formatRelativeTime(customer.lastInteractionAtUtc) }}</span>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.widget {
  padding: 20px;
}

.customers-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.customer-item {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-bottom: 1px solid #eee;
  cursor: pointer;
}

.customer-time {
  color: var(--muted);
  white-space: nowrap;
}

.truncate {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.skeleton {
  list-style: none;
  padding: 0;
}

.skeleton li {
  height: 2rem;
  margin-bottom: 0.5rem;
  background: #eee;
}
</style>
