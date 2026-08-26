<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'

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

    <p v-if="justSubmitted && store.currentTicket" class="surface success-banner" role="status">
      {{ t('portal.ticket.submit.success', { id: store.currentTicket.id }) }}
    </p>

    <p v-if="store.loading">{{ t('portal.tickets.loading') }}</p>

    <div v-else-if="!id || (!store.currentTicket && !store.error)">
      <p>{{ t('portal.errors.notFound') }}</p>
      <button type="button" @click="onBack">{{ t('portal.ticket.details.backToList') }}</button>
    </div>

    <div v-else-if="store.error" role="alert">
      <p>{{ t('portal.tickets.error') }}</p>
      <button type="button" @click="loadTicket">{{ t('portal.tickets.retry') }}</button>
    </div>

    <div v-else-if="store.currentTicket">
      <header class="ticket-header">
        <h2>{{ store.currentTicket.title }}</h2>
      </header>

      <section>
        <p>
          {{ t('portal.ticket.details.status') }}:
          {{ t(`tickets.statuses.${store.currentTicket.status}`) }}
        </p>
        <p>
          {{ t('tickets.details.priority') }}:
          {{ t(`tickets.priorities.${store.currentTicket.priority}`) }}
        </p>
      </section>

      <section>
        <p>{{ store.currentTicket.description }}</p>
      </section>

      <section>
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

      <button type="button" @click="onBack">{{ t('portal.ticket.details.backToList') }}</button>
    </div>
  </div>
</template>

<style scoped>
.portal-ticket-details-view {
  max-width: 40rem;
  margin: 4rem auto;
}

.success-banner {
  padding: 12px 16px;
  margin-bottom: 18px;
  color: #1a7a3a;
  background: #e3f6e8;
  border-radius: 6px;
}

.conversation,
.history {
  margin-top: 1rem;
  padding: 1rem;
}

.message-list,
.history-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.message-meta {
  display: block;
  opacity: 0.75;
  font-size: 0.85rem;
}
</style>
