<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useRelativeTime } from '@/composables/useRelativeTime'
import type { TicketListItem } from '@/types/tickets'

defineProps<{ tickets: TicketListItem[]; loading: boolean }>()

const { t } = useI18n()
const router = useRouter()
const { formatRelativeTime } = useRelativeTime()

function onRowClick(id: string) {
  router.push({ name: 'ticket-details', params: { id } })
}
</script>

<template>
  <section class="surface widget">
    <h3>{{ t('dashboard.widgets.myTasks') }}</h3>

    <ul v-if="loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="tickets.length === 0" class="empty-state">
      <p>{{ t('dashboard.empty.myTasks') }}</p>
    </div>

    <div v-else class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('tickets.list.columns.title') }}</th>
            <th>{{ t('tickets.list.columns.customer') }}</th>
            <th>{{ t('tickets.list.columns.priority') }}</th>
            <th>{{ t('tickets.list.columns.status') }}</th>
            <th>{{ t('dashboard.updatedAt') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="ticket in tickets"
            :key="ticket.id"
            class="clickable-row"
            @click="onRowClick(ticket.id)"
          >
            <td class="truncate" :title="ticket.title">{{ ticket.title }}</td>
            <td class="truncate" :title="ticket.customerName">{{ ticket.customerName }}</td>
            <td>{{ t(`tickets.priorities.${ticket.priority}`) }}</td>
            <td>{{ t(`tickets.statuses.${ticket.status}`) }}</td>
            <td>{{ formatRelativeTime(ticket.updatedAtUtc) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped>
.widget {
  padding: 20px;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  text-align: start;
  padding: 0.5rem;
}

.clickable-row {
  cursor: pointer;
}

.truncate {
  max-width: 16rem;
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
