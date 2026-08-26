<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTicketsStore } from '@/stores/tickets'
import { useLocale } from '@/composables/useLocale'
import SlaBadge from '@/modules/tickets/components/SlaBadge.vue'
import type { SlaStatus, TicketPriority, TicketSla, TicketStatus } from '@/types/tickets'

const SLA_SEVERITY: Record<SlaStatus, number> = {
  Breached: 4,
  AtRisk: 3,
  OnTrack: 2,
  Met: 1,
  NotApplicable: 0,
}

function worseSlaKind(sla: TicketSla): 'firstResponse' | 'resolution' {
  return SLA_SEVERITY[sla.resolutionStatus] >= SLA_SEVERITY[sla.firstResponseStatus]
    ? 'resolution'
    : 'firstResponse'
}

const { t } = useI18n()
const { locale } = useLocale()
const router = useRouter()
const store = useTicketsStore()

const STATUSES: TicketStatus[] = ['Open', 'InProgress', 'Resolved', 'Closed']
const PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

const totalPages = computed(() => Math.max(1, Math.ceil(store.total / store.pageSize)))

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function onSearchInput(event: Event) {
  store.setSearch((event.target as HTMLInputElement).value)
}

function onStatusChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as TicketStatus | ''
  store.setFilters({ status: value })
}

function onPriorityChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as TicketPriority | ''
  store.setFilters({ priority: value })
}

function onPrev() {
  if (store.page > 1) {
    store.setPage(store.page - 1)
  }
}

function onNext() {
  if (store.page < totalPages.value) {
    store.setPage(store.page + 1)
  }
}

function onRowClick(id: string) {
  router.push({ name: 'ticket-details', params: { id } })
}

onMounted(() => {
  void store.fetchList()
})
</script>

<template>
  <div class="tickets-list-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('tickets.title') }}</h1>
      </div>
      <router-link class="button" :to="{ name: 'ticket-create' }">
        {{ t('tickets.list.newButton') }}
      </router-link>
    </div>

    <div class="surface toolbar">
      <div class="toolbar-field">
        <label for="ticket-search">{{ t('common.search') }}</label>
        <input
          id="ticket-search"
          type="search"
          :placeholder="t('tickets.list.search')"
          :value="store.search"
          @input="onSearchInput"
        />
      </div>

      <div class="toolbar-field">
        <label for="ticket-status">{{ t('tickets.list.status') }}</label>
        <select id="ticket-status" :value="store.status" @change="onStatusChange">
          <option value="">{{ t('tickets.list.allStatuses') }}</option>
          <option v-for="option in STATUSES" :key="option" :value="option">
            {{ t(`tickets.statuses.${option}`) }}
          </option>
        </select>
      </div>

      <div class="toolbar-field">
        <label for="ticket-priority">{{ t('tickets.list.priority') }}</label>
        <select id="ticket-priority" :value="store.priority" @change="onPriorityChange">
          <option value="">{{ t('tickets.list.allPriorities') }}</option>
          <option v-for="option in PRIORITIES" :key="option" :value="option">
            {{ t(`tickets.priorities.${option}`) }}
          </option>
        </select>
      </div>
    </div>

    <p v-if="store.loading">{{ t('common.loading') }}</p>
    <p v-else-if="store.error" role="alert">{{ t('tickets.errors.errorLoad') }}</p>
    <div v-else-if="store.items.length === 0" class="surface empty-state">
      <p>{{ t('tickets.list.empty') }}</p>
    </div>

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('tickets.list.columns.title') }}</th>
            <th>{{ t('tickets.list.columns.customer') }}</th>
            <th>{{ t('tickets.list.columns.status') }}</th>
            <th>{{ t('tickets.list.columns.priority') }}</th>
            <th>{{ t('tickets.list.columns.createdAt') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="ticket in store.items"
            :key="ticket.id"
            class="clickable-row"
            @click="onRowClick(ticket.id)"
          >
            <td>{{ ticket.title }}</td>
            <td>{{ ticket.customerName }}</td>
            <td>{{ t(`tickets.statuses.${ticket.status}`) }}</td>
            <td>{{ t(`tickets.priorities.${ticket.priority}`) }}</td>
            <td>{{ formatDate(ticket.createdAtUtc) }}</td>
            <td><SlaBadge :sla="ticket.sla" :kind="worseSlaKind(ticket.sla)" /></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pagination">
      <button type="button" :disabled="store.page <= 1" @click="onPrev">
        {{ t('customers.pagination.prev') }}
      </button>
      <span>{{ t('customers.pagination.pageOf', { page: store.page, totalPages }) }}</span>
      <button type="button" :disabled="store.page >= totalPages" @click="onNext">
        {{ t('customers.pagination.next') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.tickets-list-view {
  max-width: 60rem;
  margin: 4rem auto;
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

.pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1rem;
}
</style>
