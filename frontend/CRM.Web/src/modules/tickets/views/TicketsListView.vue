<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useTicketsStore } from '@/stores/tickets'
import { useLocale } from '@/composables/useLocale'
import SlaBadge from '@/modules/tickets/components/SlaBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
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

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function onStatusChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as TicketStatus | ''
  store.setFilters({ status: value })
}

function onPriorityChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as TicketPriority | ''
  store.setFilters({ priority: value })
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
      <AppButton :to="{ name: 'ticket-create' }">
        {{ t('tickets.list.newButton') }}
      </AppButton>
    </div>

    <div class="surface toolbar">
      <div class="toolbar-field">
        <AppInput
          id="ticket-search"
          :label="t('common.search')"
          type="search"
          :placeholder="t('tickets.list.search')"
          :model-value="store.search"
          @update:model-value="store.setSearch"
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

    <LoadingState v-if="store.loading" />
    <ErrorState v-else-if="store.error" :retryable="false" :message="t('tickets.errors.errorLoad')" />
    <EmptyState v-else-if="store.items.length === 0" :description="t('tickets.list.empty')" />

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

    <AppPagination
      v-if="store.items.length > 0"
      :page="store.page"
      :page-size="store.pageSize"
      :total-count="store.total"
      @update:page="store.setPage"
    />
  </div>
</template>

<style scoped>
.clickable-row {
  cursor: pointer;
}
</style>
