<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useCustomerPortalStore } from '@/stores/customerPortal'
import { useLocale } from '@/composables/useLocale'
import { useToast } from '@/composables/useToast'
import { useRelativeTime } from '@/composables/useRelativeTime'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import LoadingState from '@/components/ui/LoadingState.vue'

const MESSAGE_MAX_LENGTH = 5000

const { t } = useI18n()
const { locale } = useLocale()
const { formatRelativeTime } = useRelativeTime()
const route = useRoute()
const router = useRouter()
const store = useCustomerPortalStore()
const toast = useToast()

const id = (route.params.id as string | undefined)?.trim() ?? ''
const justSubmitted = route.query.submitted === '1'

const draftBody = ref('')
const conversationRef = ref<HTMLElement | null>(null)

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function initials(label: string): string {
  return label.trim().slice(0, 1).toUpperCase() || '?'
}

function scrollToLatest() {
  const el = conversationRef.value
  if (el) {
    el.scrollTop = el.scrollHeight
  }
}

function loadTicket() {
  if (!id) {
    return
  }
  void store.fetchTicket(id)
}

onMounted(loadTicket)

watch(
  () => store.currentTicket?.messages.length,
  () => {
    void nextTick(scrollToLatest)
  },
)

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
    <AppButton variant="ghost" size="sm" type="button" class="back-link" @click="onBack">
      ← {{ t('portal.ticket.details.backToList') }}
    </AppButton>

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
        <div class="ticket-header-top">
          <div class="ticket-heading">
            <span class="ticket-number">#{{ store.currentTicket.id.slice(0, 8) }}</span>
            <h1>{{ store.currentTicket.title }}</h1>
          </div>
          <AppButton variant="ghost" size="sm" type="button" class="refresh-btn" @click="onRefresh">
            ⟳ {{ t('portal.ticket.details.refresh') }}
          </AppButton>
        </div>

        <div class="ticket-meta-row">
          <AppBadge :tone="statusTone" class="status-badge">
            {{ t(`tickets.statuses.${store.currentTicket.status}`) }}
          </AppBadge>
          <span class="meta-chip">{{ t(`tickets.priorities.${store.currentTicket.priority}`) }}</span>
          <span class="meta-sep">·</span>
          <span class="meta-text">
            {{ t('tickets.details.createdAt') }} {{ formatDate(store.currentTicket.createdAtUtc) }}
          </span>
          <span class="meta-sep">·</span>
          <span class="meta-text">
            {{ t('tickets.details.updatedAt') }} {{ formatDate(store.currentTicket.updatedAtUtc) }}
          </span>
        </div>

        <p class="ticket-description">{{ store.currentTicket.description }}</p>
      </header>

      <section class="surface conversation-card">
        <h2 class="conversation-heading">{{ t('portal.ticket.details.conversation') }}</h2>

        <div v-if="store.currentTicket.messages.length === 0" class="conversation-empty">
          {{ t('portal.ticket.details.noMessages') }}
        </div>

        <ul v-else ref="conversationRef" class="conversation-scroll">
          <li
            v-for="message in store.currentTicket.messages"
            :key="message.id"
            class="chat-row"
            :class="message.senderType === 'Customer' ? 'chat-row--mine' : 'chat-row--support'"
          >
            <span class="chat-avatar" aria-hidden="true">
              {{ initials(t(`portal.ticket.details.conversationRoles.${message.senderType === 'Customer' ? 'customer' : 'support'}`)) }}
            </span>
            <div class="chat-bubble-wrap">
              <span class="chat-sender">
                {{ t(`portal.ticket.details.conversationRoles.${message.senderType === 'Customer' ? 'customer' : 'support'}`) }}
              </span>
              <div class="chat-bubble">{{ message.body }}</div>
              <span class="chat-time" :title="formatDate(message.createdAtUtc)">
                {{ formatRelativeTime(message.createdAtUtc) }}
              </span>
            </div>
          </li>
        </ul>

        <AppAlert v-if="store.replyError" tone="danger" role="alert">
          {{ t(`portal.ticket.details.composer.errors.${store.replyError}`) }}
        </AppAlert>

        <form v-if="!isClosed" class="composer" @submit.prevent="onSend">
          <label for="portal-reply-body" class="sr-only">{{ t('portal.ticket.details.composer.label') }}</label>
          <textarea
            id="portal-reply-body"
            v-model="draftBody"
            :maxlength="MESSAGE_MAX_LENGTH"
            :disabled="store.sendingReply"
            :placeholder="t('portal.ticket.details.composer.placeholder')"
            rows="2"
          ></textarea>
          <div class="composer-actions">
            <AppButton type="submit" size="sm" :disabled="!canSend" :loading="store.sendingReply">
              {{ store.sendingReply ? t('portal.ticket.details.composer.sending') : t('portal.ticket.details.composer.send') }}
            </AppButton>
          </div>
        </form>

        <div v-else class="closed-composer">
          <p class="text-body">{{ t('portal.ticket.details.closed.body') }}</p>
          <AppButton variant="primary" type="button" :to="{ name: 'portal-ticket-create' }">
            {{ t('portal.ticket.details.closed.cta') }}
          </AppButton>
        </div>
      </section>

      <section class="surface history-card">
        <h2 class="conversation-heading">{{ t('tickets.history.title') }}</h2>
        <p v-if="store.currentTicket.history.length === 0" class="conversation-empty">
          {{ t('tickets.history.empty') }}
        </p>
        <ul v-else class="history-list">
          <li v-for="entry in store.currentTicket.history" :key="entry.id">
            <span class="history-dot" aria-hidden="true"></span>
            {{ historyLine(entry) }}
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>

<style scoped>
.portal-ticket-details-view {
  max-width: 44rem;
  margin: var(--space-6) auto var(--space-8);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.back-link {
  align-self: flex-start;
  padding-inline: var(--space-2);
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
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
  gap: var(--space-4);
}

.ticket-header {
  padding: var(--space-5) var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  border-inline-start: 4px solid var(--accent);
}

.ticket-header-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
}

.ticket-heading {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.ticket-number {
  font-size: var(--font-size-xs);
  font-weight: 700;
  letter-spacing: 0.03em;
  color: var(--muted);
}

.ticket-heading h1 {
  margin: 0;
  font-size: var(--font-size-xl);
  line-height: 1.25;
}

.refresh-btn {
  flex: none;
  white-space: nowrap;
}

.ticket-meta-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--font-size-sm);
  color: var(--muted);
}

.meta-chip {
  padding: 0.15rem var(--space-2);
  background: var(--canvas);
  border-radius: var(--radius-sm);
  font-weight: 600;
  color: var(--text-primary);
}

.meta-sep {
  color: var(--line);
}

.ticket-description {
  margin: 0;
  padding-top: var(--space-2);
  border-top: 1px solid var(--line);
  color: var(--text-primary);
  line-height: 1.55;
  white-space: pre-wrap;
}

.conversation-card,
.history-card {
  padding: var(--space-5) var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.conversation-heading {
  margin: 0;
  font-size: var(--font-size-md);
}

.conversation-empty {
  padding: var(--space-4) 0;
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.conversation-scroll {
  list-style: none;
  margin: 0;
  padding: 0;
  padding-inline-end: var(--space-1);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  max-height: 26rem;
  overflow-y: auto;
}

.chat-row {
  display: flex;
  align-items: flex-start;
  gap: var(--space-2);
  max-width: 85%;
}

.chat-row--support {
  align-self: flex-start;
}

.chat-row--mine {
  align-self: flex-end;
  flex-direction: row-reverse;
}

.chat-avatar {
  flex: none;
  width: 1.75rem;
  height: 1.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: var(--color-status-info-bg);
  color: var(--color-status-info);
  font-size: var(--font-size-xs);
  font-weight: 700;
}

.chat-row--mine .chat-avatar {
  background: var(--accent);
  color: white;
}

.chat-bubble-wrap {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.chat-row--mine .chat-bubble-wrap {
  align-items: flex-end;
}

.chat-sender {
  font-size: var(--font-size-xs);
  font-weight: 700;
  color: var(--muted);
}

.chat-bubble {
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-lg);
  background: var(--canvas);
  color: var(--text-primary);
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
}

.chat-row--mine .chat-bubble {
  background: var(--accent);
  color: white;
  border-end-end-radius: var(--radius-sm);
}

.chat-row--support .chat-bubble {
  border-end-start-radius: var(--radius-sm);
}

.chat-time {
  font-size: var(--font-size-xs);
  color: var(--text-muted);
  padding-inline: 0.15rem;
}

.composer {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding-top: var(--space-3);
  border-top: 1px solid var(--line);
}

.composer textarea {
  width: 100%;
  box-sizing: border-box;
  padding: var(--space-3);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  font: 400 14px var(--font-sans, Arial, sans-serif);
  resize: vertical;
}

.composer textarea:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 1px;
}

.composer-actions {
  display: flex;
  justify-content: flex-end;
}

.closed-composer {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-3);
  padding-top: var(--space-3);
  border-top: 1px solid var(--line);
}

.history-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.history-list li {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--font-size-sm);
  color: var(--muted);
}

.history-dot {
  flex: none;
  width: 0.4rem;
  height: 0.4rem;
  border-radius: 50%;
  background: var(--line);
}

/* Logical properties throughout (padding-inline/margin-inline, flex gap,
   border-inline-start, border-end-start/end-radius) so RTL (dir="rtl", set
   globally per assets/main.css) mirrors correctly with no per-component
   RTL overrides needed here. */
@media (max-width: 640px) {
  .portal-ticket-details-view {
    margin: var(--space-4) auto;
  }

  .ticket-header-top {
    flex-direction: column;
    align-items: flex-start;
  }

  .chat-row {
    max-width: 100%;
  }

  .conversation-scroll {
    max-height: 20rem;
  }
}
</style>
