<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const { t } = useI18n()
const { locale } = useLocale()
const router = useRouter()
const store = useCustomerPortalStore()

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function loadTickets() {
  void store.fetchTickets()
}

function onRowClick(id: string) {
  router.push({ name: 'portal-ticket-details', params: { id } })
}

onMounted(loadTickets)
</script>

<template>
  <div class="portal-tickets-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('portal.dashboard.overline') }}</p>
        <h1>{{ t('portal.tickets.title') }}</h1>
      </div>
      <AppButton :to="{ name: 'portal-ticket-create' }">
        {{ t('portal.dashboard.submitCta') }}
      </AppButton>
    </div>

    <LoadingState v-if="store.loading" :label="t('portal.tickets.loading')" />
    <AppAlert v-else-if="store.error" tone="danger" class="portal-error">
      {{ t('portal.tickets.error') }}
      <AppButton variant="secondary" size="sm" type="button" @click="loadTickets">{{ t('portal.tickets.retry') }}</AppButton>
    </AppAlert>
    <EmptyState v-else-if="store.tickets.length === 0" :title="t('portal.tickets.empty')">
      <AppButton :to="{ name: 'portal-ticket-create' }">
        {{ t('portal.tickets.emptyCta') }}
      </AppButton>
    </EmptyState>

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('tickets.list.columns.title') }}</th>
            <th>{{ t('tickets.list.columns.status') }}</th>
            <th>{{ t('tickets.list.columns.priority') }}</th>
            <th>{{ t('tickets.list.columns.createdAt') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="ticket in store.tickets"
            :key="ticket.id"
            class="clickable-row"
            @click="onRowClick(ticket.id)"
          >
            <td>{{ ticket.title }}</td>
            <td>{{ t(`tickets.statuses.${ticket.status}`) }}</td>
            <td>{{ t(`tickets.priorities.${ticket.priority}`) }}</td>
            <td>{{ formatDate(ticket.createdAtUtc) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.portal-tickets-view {
  max-width: 60rem;
  margin: var(--space-8) auto;
}

.portal-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.clickable-row {
  cursor: pointer;
}
</style>
