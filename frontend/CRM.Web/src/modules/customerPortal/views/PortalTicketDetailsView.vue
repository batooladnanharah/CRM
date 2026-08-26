<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import LoadingState from '@/components/ui/LoadingState.vue'

const { t } = useI18n()
const { locale } = useLocale()
const route = useRoute()
const router = useRouter()
const store = useCustomerPortalStore()

const id = (route.params.id as string | undefined)?.trim() ?? ''
const justSubmitted = route.query.submitted === '1'

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function loadTicket() {
  if (!id) {
    return
  }
  void store.fetchTicket(id)
}

onMounted(loadTicket)

function onBack() {
  router.push({ name: 'portal-tickets-list' })
}

function historyLine(entry: NonNullable<typeof store.currentTicket>['history'][number]): string {
  const when = formatDate(entry.changedAtUtc)
  const from = entry.oldValue ? t(`tickets.statuses.${entry.oldValue}`) : t('portal.ticket.details.none')
  const to = entry.newValue ? t(`tickets.statuses.${entry.newValue}`) : t('portal.ticket.details.none')
  return t('portal.ticket.details.statusChange', { from, to, when })
}
</script>

<template>
  <div class="portal-ticket-details-view">
    <h1>{{ t('portal.ticket.details.title') }}</h1>

    <AppAlert v-if="justSubmitted && store.currentTicket" tone="success" role="status">
      {{ t('portal.ticket.submit.success', { id: store.currentTicket.id }) }}
    </AppAlert>

    <LoadingState v-if="store.loading" :label="t('portal.tickets.loading')" />

    <div v-else-if="!id || (!store.currentTicket && !store.error)" class="surface state-card">
      <p class="text-body">{{ t('portal.errors.notFound') }}</p>
      <AppButton variant="secondary" type="button" @click="onBack">{{ t('portal.ticket.details.backToList') }}</AppButton>
    </div>

    <div v-else-if="store.error" class="surface state-card" role="alert">
      <p class="text-body">{{ t('portal.tickets.error') }}</p>
      <AppButton variant="secondary" type="button" @click="loadTicket">{{ t('portal.tickets.retry') }}</AppButton>
    </div>

    <div v-else-if="store.currentTicket" class="ticket-detail-body">
      <header class="surface ticket-header">
        <h2>{{ store.currentTicket.title }}</h2>
      </header>

      <section class="surface ticket-section">
        <p>
          {{ t('portal.ticket.details.status') }}:
          {{ t(`tickets.statuses.${store.currentTicket.status}`) }}
        </p>
        <p>
          {{ t('tickets.details.priority') }}:
          {{ t(`tickets.priorities.${store.currentTicket.priority}`) }}
        </p>
      </section>

      <section class="surface ticket-section">
        <p>{{ store.currentTicket.description }}</p>
      </section>

      <section class="surface ticket-section">
        <p>{{ t('tickets.details.createdAt') }}: {{ formatDate(store.currentTicket.createdAtUtc) }}</p>
        <p>{{ t('tickets.details.updatedAt') }}: {{ formatDate(store.currentTicket.updatedAtUtc) }}</p>
      </section>

      <section class="surface conversation">
        <h3>{{ t('portal.ticket.details.conversation') }}</h3>
        <p v-if="store.currentTicket.messages.length === 0">{{ t('portal.ticket.details.noMessages') }}</p>
        <ul v-else class="message-list">
          <li v-for="message in store.currentTicket.messages" :key="message.id">
            <p>{{ message.body }}</p>
            <span class="message-meta">{{ formatDate(message.createdAtUtc) }}</span>
          </li>
        </ul>
      </section>

      <section class="surface history">
        <h3>{{ t('tickets.history.title') }}</h3>
        <p v-if="store.currentTicket.history.length === 0">{{ t('tickets.history.empty') }}</p>
        <ul v-else class="history-list">
          <li v-for="entry in store.currentTicket.history" :key="entry.id">{{ historyLine(entry) }}</li>
        </ul>
      </section>

      <AppButton variant="ghost" type="button" @click="onBack">{{ t('portal.ticket.details.backToList') }}</AppButton>
    </div>
  </div>
</template>

<style scoped>
.portal-ticket-details-view {
  max-width: 40rem;
  margin: var(--space-8) auto;
}

.state-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-6);
}

.ticket-detail-body {
  display: flex;
  flex-direction: column;
  gap: var(--space-5);
}

.ticket-header,
.ticket-section,
.conversation,
.history {
  padding: var(--space-5) var(--space-6);
}

.message-list,
.history-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  margin-top: var(--space-3);
}

.message-meta {
  display: block;
  color: var(--muted);
  font-size: var(--font-size-sm);
}
</style>
