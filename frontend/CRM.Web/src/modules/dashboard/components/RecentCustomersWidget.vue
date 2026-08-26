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
  padding: var(--space-5);
}

.widget h3 {
  margin: 0 0 var(--space-4);
}

.customers-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.customer-item {
  display: flex;
  justify-content: space-between;
  gap: var(--space-2);
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--line);
  cursor: pointer;
  border-radius: var(--radius-sm);
}

.customer-item:hover {
  background: #f5fbf9;
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
  margin-bottom: var(--space-2);
  background: var(--canvas);
  border-radius: var(--radius-sm);
}
</style>
