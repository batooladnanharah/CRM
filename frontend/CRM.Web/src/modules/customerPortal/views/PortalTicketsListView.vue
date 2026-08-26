<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'

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
      <router-link class="button" :to="{ name: 'portal-ticket-create' }">
        {{ t('portal.dashboard.submitCta') }}
      </router-link>
    </div>

    <p v-if="store.loading">{{ t('portal.tickets.loading') }}</p>
    <p v-else-if="store.error" role="alert" class="portal-error">
      {{ t('portal.tickets.error') }}
      <button type="button" @click="loadTickets">{{ t('portal.tickets.retry') }}</button>
    </p>
    <div v-else-if="store.tickets.length === 0" class="surface empty-state">
      <p>{{ t('portal.tickets.empty') }}</p>
      <router-link class="button" :to="{ name: 'portal-ticket-create' }">
        {{ t('portal.tickets.emptyCta') }}
      </router-link>
    </div>

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
  margin: 4rem auto;
}

.portal-error {
  padding: 12px 16px;
  margin-bottom: 18px;
  color: #b00020;
  background: #fdecea;
  border-radius: 6px;
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
</style>
