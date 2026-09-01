<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import { useToast } from '@/composables/useToast'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import LoadingState from '@/components/ui/LoadingState.vue'

const MESSAGE_MAX_LENGTH = 5000

const { t } = useI18n()
const { locale } = useLocale()
const route = useRoute()
const router = useRouter()
const store = useCustomerPortalStore()
const toast = useToast()

const id = (route.params.id as string | undefined)?.trim() ?? ''
const justSubmitted = route.query.submitted === '1'

const draftBody = ref('')

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

function onRefresh() {
  if (!id) {
    return
  }
  void store.refreshTicket(id)
}

function historyLine(entry: NonNullable<typeof store.currentTicket>['history'][number]): string {
  const when = formatDate(entry.changedAtUtc)
  const from = entry.oldValue ? t(`tickets.statuses.${entry.oldValue}`) : t('portal.ticket.details.none')
  const to = entry.newValue ? t(`tickets.statuses.${entry.newValue}`) : t('portal.ticket.details.none')
  return t('portal.ticket.details.statusChange', { from, to, when })
}

// Mirrors the backend rule in CustomerPortalEndpoints (TicketStatusRules:
// Closed is terminal) for UX only — the server is the source of truth and
// re-checks this on every POST regardless of what the client shows here.
const isClosed = computed(() => store.currentTicket?.status === 'Closed')

const canSend = computed(() => !store.sendingReply && draftBody.value.trim().length > 0)

const statusTone = computed<'neutral' | 'success' | 'warning' | 'danger'>(() => {
  switch (store.currentTicket?.status) {
    case 'Resolved':
      return 'success'
    case 'Closed':
      return 'neutral'
    case 'InProgress':
      return 'warning'
    default:
      return 'neutral'
  }
})

async function onSend() {
  const body = draftBody.value.trim()
  if (!body || !id) {
    return
  }

  try {
    await store.sendReply(id, body)
    draftBody.value = ''
    toast.success(t('portal.ticket.details.composer.toastSent'))
  } catch {
    toast.error(t('portal.ticket.details.composer.toastError'))
  }
}
</script>

<template>
  <div class="portal-ticket-details-view">
    <h1>{{ t('portal.ticket.details.title') }}</h1>

    <AppAlert v-if="justSubmitted && store.currentTicket" tone="success" role="status">
      {{ t('portal.ticket.submit.success', { id: store.currentTicket.id }) }}
    </AppAlert>

    <LoadingState v-if="store.loading" :label="t('portal.tickets.loading')" />

    <div v-else-if="!id || store.notFound || (!store.currentTicket && !store.error)" class="surface state-card">
      <p class="text-body">{{ t('portal.errors.notFound') }}</p>
      <AppButton variant="secondary" type="button" @click="onBack">{{ t('portal.ticket.details.backToList') }}</AppButton>
    </div>

    <div v-else-if="store.error" class="surface state-card" role="alert">
      <p class="text-body">{{ t('portal.tickets.error') }}</p>
      <AppButton variant="secondary" type="button" @click="loadTicket">{{ t('portal.tickets.retry') }}</AppButton>
    </div>

    <div v-else-if="store.currentTicket" class="ticket-detail-body">
      <header class="surface ticket-header">
        <div class="ticket-header-row">
          <h2>{{ store.currentTicket.title }}</h2>
          <AppButton variant="ghost" size="sm" type="button" @click="onRefresh">
            {{ t('portal.ticket.details.refresh') }}
          </AppButton>
        </div>
      </header>

      <section class="surface ticket-section">
        <p>
          {{ t('portal.ticket.details.status') }}:
          <AppBadge :tone="statusTone">{{ t(`tickets.statuses.${store.currentTicket.status}`) }}</AppBadge>
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
            <p class="message-meta">
              <span class="message-sender">
                {{ t(`portal.ticket.details.conversationRoles.${message.senderType === 'Customer' ? 'customer' : 'support'}`) }}
              </span>
              <span>{{ formatDate(message.createdAtUtc) }}</span>
            </p>
            <p>{{ message.body }}</p>
          </li>
        </ul>

        <AppAlert v-if="store.replyError" tone="danger" role="alert">
          {{ t(`portal.ticket.details.composer.errors.${store.replyError}`) }}
        </AppAlert>

        <form v-if="!isClosed" class="composer" @submit.prevent="onSend">
          <label for="portal-reply-body">{{ t('portal.ticket.details.composer.label') }}</label>
          <textarea
            id="portal-reply-body"
            v-model="draftBody"
            :maxlength="MESSAGE_MAX_LENGTH"
            :disabled="store.sendingReply"
            :placeholder="t('portal.ticket.details.composer.placeholder')"
            rows="3"
          ></textarea>
          <div class="composer-actions">
            <AppButton type="submit" size="sm" :disabled="!canSend">
              {{ store.sendingReply ? t('portal.ticket.details.composer.sending') : t('portal.ticket.details.composer.send') }}
            </AppButton>
          </div>
        </form>

        <div v-else class="surface state-card closed-composer">
          <p class="text-body">{{ t('portal.ticket.details.closed.body') }}</p>
          <AppButton variant="primary" type="button" :to="{ name: 'portal-ticket-create' }">
            {{ t('portal.ticket.details.closed.cta') }}
          </AppButton>
        </div>
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
  display: flex;
  gap: var(--space-3);
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.message-sender {
  font-weight: 700;
}

.ticket-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
}

.composer {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-top: var(--space-4);
}

.composer textarea {
  width: 100%;
  box-sizing: border-box;
  padding: var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  font: 400 14px var(--font-sans, Arial, sans-serif);
}

.composer-actions {
  display: flex;
  justify-content: flex-end;
}

.closed-composer {
  margin-top: var(--space-4);
  gap: var(--space-3);
}

/* Logical properties throughout (padding-inline/margin-inline, flex gap) so
   RTL (dir="rtl", set globally per assets/main.css) mirrors correctly with
   no per-component RTL overrides needed here. */
@media (max-width: 640px) {
  .portal-ticket-details-view {
    margin: var(--space-4) auto;
  }

  .ticket-header-row {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
