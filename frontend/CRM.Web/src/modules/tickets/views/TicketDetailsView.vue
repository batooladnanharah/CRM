<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useTicketsStore } from '@/stores/tickets'
import { useAuthStore } from '@/stores/auth'
import { useAiStore } from '@/stores/ai'
import { useLocale } from '@/composables/useLocale'
import { isEscalatable, legalNextStatuses } from '@/modules/tickets/statusTransitions'
import TicketMessagesSection from '@/modules/tickets/components/TicketMessagesSection.vue'
import TicketAttachmentsSection from '@/modules/tickets/components/TicketAttachmentsSection.vue'
import EscalateTicketDialog from '@/modules/tickets/components/EscalateTicketDialog.vue'
import SlaBadge from '@/modules/tickets/components/SlaBadge.vue'
import KnowledgeBaseSearchDialog from '@/modules/knowledgeBase/components/KnowledgeBaseSearchDialog.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import type { TicketPriority, TicketStatus } from '@/types/tickets'

const { t, te } = useI18n()
const { locale } = useLocale()
const route = useRoute()
const router = useRouter()
const store = useTicketsStore()
const authStore = useAuthStore()
const aiStore = useAiStore()

const id = (route.params.id as string | undefined)?.trim() ?? ''

const PRIORITIES: TicketPriority[] = ['Low', 'Normal', 'High', 'Urgent']

const canManage = computed(() => authStore.isAdmin || authStore.isAgent)
const canEscalate = computed(
  () => authStore.isAdmin && !!store.current && isEscalatable(store.current.status),
)
const historyOpened = ref(false)
const knowledgeBaseSearchOpen = ref(false)

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

const nextStatuses = computed(() =>
  store.current ? legalNextStatuses(store.current.status) : [],
)

const actionErrorText = computed(() => {
  if (!store.actionError) {
    return null
  }
  const key = `tickets.errors.${store.actionError}`
  return te(key) ? t(key) : store.actionError
})

function historyLine(entry: (typeof store.history)[number]): string {
  const name = entry.isSystemActor
    ? t('tickets.history.systemActor')
    : entry.changedByDisplayName || t('tickets.history.unknownActor')
  const when = formatDate(entry.changedAtUtc)

  if (entry.changeType === 'Assignment') {
    return entry.newValue
      ? t('tickets.history.assignment', { name, when })
      : t('tickets.history.unassignment', { name, when })
  }
  if (entry.changeType === 'Status') {
    return t('tickets.history.statusChange', {
      name,
      from: t(`tickets.statuses.${entry.oldValue}`),
      to: t(`tickets.statuses.${entry.newValue}`),
      when,
    })
  }
  if (entry.changeType === 'Priority') {
    return t('tickets.history.priorityChange', {
      name,
      from: t(`tickets.priorities.${entry.oldValue}`),
      to: t(`tickets.priorities.${entry.newValue}`),
      when,
    })
  }
  if (entry.changeType === 'MessageAdded') {
    return t('tickets.history.messageAdded', { name, when })
  }
  if (entry.changeType === 'AttachmentAdded') {
    return t('tickets.history.attachmentAdded', { name, when })
  }
  if (entry.changeType === 'AttachmentRemoved') {
    return t('tickets.history.attachmentRemoved', { name, when })
  }
  if (entry.changeType === 'SlaRecalculated') {
    return t('tickets.history.slaRecalculated', { when })
  }
  if (entry.changeType === 'SlaBreached') {
    return t('tickets.history.slaBreached', {
      kind: t(`tickets.history.slaBreachKinds.${entry.newValue}`),
      when,
    })
  }
  if (entry.changeType === 'Escalated' && entry.isSystemActor) {
    return t('tickets.history.autoEscalated', {
      from: t(`tickets.priorities.${entry.oldValue}`),
      to: t(`tickets.priorities.${entry.newValue}`),
      when,
    })
  }
  return t('tickets.history.escalated', {
    name,
    from: t(`tickets.priorities.${entry.oldValue}`),
    to: t(`tickets.priorities.${entry.newValue}`),
    reason: entry.reason ?? '',
    when,
  })
}

function loadTicket() {
  if (!id) {
    return
  }
  void store.fetchOne(id)
}

onMounted(() => {
  loadTicket()
  if (canManage.value) {
    void store.loadEligibleAgents()
  }
})

function onBack() {
  router.push({ name: 'tickets-list' })
}

function onRetry() {
  loadTicket()
}

async function onAssigneeChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  try {
    await store.assign(id, value || null)
  } catch {
    // store.actionError already carries the message to display.
  }
}

async function onStatusChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as TicketStatus
  try {
    await store.changeStatus(id, value)
  } catch {
    // store.actionError already carries the message to display.
  }
}

async function onPriorityChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as TicketPriority
  try {
    await store.changePriority(id, value)
  } catch {
    // store.actionError already carries the message to display.
  }
}

function onHistoryToggle(event: Event) {
  historyOpened.value = (event.target as HTMLDetailsElement).open
  if (historyOpened.value && store.history.length === 0) {
    void store.loadHistory(id)
  }
}

const aiAvailable = computed(() => aiStore.status?.available ?? false)
const aiProvider = computed(() => aiStore.status?.provider ?? null)
const aiSummary = computed(() => aiStore.summaries[id])
const aiSummaryLoading = computed(() => aiStore.summaryLoading[id] ?? false)
const aiSummaryError = computed(() => aiStore.summaryError[id])

async function onGenerateSummary() {
  try {
    await aiStore.generateSummary(id)
  } catch {
    // error surfaced via aiStore.summaryError
  }
}

async function onRegenerateSummary() {
  try {
    await aiStore.regenerateSummary(id)
  } catch {
    // error surfaced via aiStore.summaryError
  }
}
</script>

<template>
  <div class="ticket-details-view">
    <h1>{{ t('tickets.details.title') }}</h1>

    <LoadingState v-if="store.loadingCurrent" :label="t('common.loading')" />

    <div v-else-if="!id || store.notFound" class="surface state-card">
      <p class="text-heading-3">{{ t('tickets.details.notFoundTitle') }}</p>
      <AppButton variant="secondary" type="button" @click="onBack">{{ t('tickets.details.backToList') }}</AppButton>
    </div>

    <div v-else-if="store.loadError" class="surface state-card" role="alert">
      <p class="text-heading-3">{{ t('tickets.details.errorLoadTitle') }}</p>
      <AppButton variant="secondary" type="button" @click="onRetry">{{ t('tickets.details.retry') }}</AppButton>
    </div>

    <div v-else-if="store.current" class="ticket-detail-body">
      <header class="surface ticket-header">
        <h2>{{ store.current.title }}</h2>
        <div class="sla-badges">
          <SlaBadge :sla="store.current.sla" kind="firstResponse" />
          <SlaBadge :sla="store.current.sla" kind="resolution" />
        </div>
        <ul v-if="store.current.escalations?.length" class="escalation-surface">
          <li v-for="(escalation, index) in store.current.escalations" :key="index">
            <span v-if="escalation.agentNotified">✓ {{ t('sla.escalation.ticket.agentNotified') }}</span>
            <span v-if="escalation.managerNotified">✓ {{ t('sla.escalation.ticket.managerNotified') }}</span>
          </li>
        </ul>
        <EscalateTicketDialog v-if="canEscalate" :ticket-id="id" />
        <AppButton
          v-if="canManage"
          variant="secondary"
          size="sm"
          type="button"
          @click="knowledgeBaseSearchOpen = true"
        >
          {{ t('tickets.details.searchKnowledgeBase') }}
        </AppButton>
      </header>

      <AppAlert v-if="actionErrorText" tone="danger" role="alert">{{ actionErrorText }}</AppAlert>

      <section class="surface ticket-section">
        <p>
          {{ t('tickets.details.status') }}:
          <template v-if="canManage">
            <select
              :value="store.current.status"
              :disabled="store.isChangingStatus"
              @change="onStatusChange"
            >
              <option v-for="option in nextStatuses" :key="option" :value="option">
                {{ t(`tickets.statuses.${option}`) }}
              </option>
            </select>
            <span v-if="store.isChangingStatus">{{ t('common.loading') }}</span>
          </template>
          <template v-else>{{ t(`tickets.statuses.${store.current.status}`) }}</template>
        </p>

        <p>
          {{ t('tickets.details.priority') }}:
          <template v-if="canManage">
            <select
              :value="store.current.priority"
              :disabled="store.isChangingPriority"
              @change="onPriorityChange"
            >
              <option v-for="option in PRIORITIES" :key="option" :value="option">
                {{ t(`tickets.priorities.${option}`) }}
              </option>
            </select>
            <span v-if="store.isChangingPriority">{{ t('common.loading') }}</span>
          </template>
          <template v-else>{{ t(`tickets.priorities.${store.current.priority}`) }}</template>
        </p>

        <p>
          {{ t('tickets.assignee') }}:
          <template v-if="canManage">
            <select
              :value="store.current.assigneeUserId ?? ''"
              :disabled="store.isAssigning"
              @change="onAssigneeChange"
            >
              <option value="">{{ t('tickets.unassigned') }}</option>
              <option v-for="agent in store.eligibleAgents" :key="agent.id" :value="agent.id">
                {{ agent.displayName }}
              </option>
            </select>
            <span v-if="store.isAssigning">{{ t('common.loading') }}</span>
          </template>
          <template v-else-if="store.current.assigneeDisplayName">{{ store.current.assigneeDisplayName }}</template>
          <template v-else>{{ t('tickets.unassigned') }}</template>
          <span v-if="store.current.assigneeUserId && store.current.autoAssigned" class="auto-assigned-badge">
            ✓ {{ t('tickets.autoAssigned') }}
          </span>
        </p>

        <p>
          {{ t('tickets.details.customer') }}:
          <router-link :to="{ name: 'customer-profile', params: { id: store.current.customerId } }">
            {{ store.current.customerName }}
          </router-link>
        </p>
      </section>

      <section class="surface ticket-section">
        <p>{{ store.current.description }}</p>
      </section>

      <section class="surface ticket-section">
        <p>{{ t('tickets.details.createdAt') }}: {{ formatDate(store.current.createdAtUtc) }}</p>
        <p>{{ t('tickets.details.updatedAt') }}: {{ formatDate(store.current.updatedAtUtc) }}</p>
      </section>

      <section class="surface ticket-section ai-assistance">
        <div class="ai-assistance__header">
          <h3>{{ t('ai.ticket.assistanceTitle') }}</h3>
          <AppBadge v-if="aiAvailable && aiProvider === 'Development'" tone="info">
            {{ t('ai.ticket.developmentBadge') }}
          </AppBadge>
        </div>

        <AppAlert v-if="!aiAvailable" tone="warning">{{ t('ai.ticket.unavailable') }}</AppAlert>

        <template v-else>
          <div class="ai-assistance__actions">
            <AppButton
              type="button"
              size="sm"
              :disabled="aiSummaryLoading"
              @click="onGenerateSummary"
            >{{ t('ai.ticket.generateSummary') }}</AppButton>
          </div>

          <LoadingState v-if="aiSummaryLoading" :label="t('ai.ticket.generatingSummary')" />

          <div v-else-if="aiSummary" class="ai-assistance__summary">
            <p class="text-body">{{ aiSummary }}</p>
            <p class="text-caption">{{ t('ai.ticket.generatedByAi') }}</p>
            <AppButton type="button" variant="secondary" size="sm" @click="onRegenerateSummary">
              {{ t('ai.ticket.regenerate') }}
            </AppButton>
          </div>

          <div v-else-if="aiSummaryError" class="ai-assistance__summary">
            <AppAlert tone="danger">{{ t('ai.ticket.summaryError') }}</AppAlert>
            <AppButton type="button" size="sm" @click="onGenerateSummary">
              {{ t('ai.ticket.tryAgain') }}
            </AppButton>
          </div>
        </template>
      </section>

      <TicketMessagesSection :ticket-id="id" :ticket="store.current" />

      <TicketAttachmentsSection :ticket-id="id" />

      <details class="surface history-panel" @toggle="onHistoryToggle">
        <summary>{{ t('tickets.history.title') }}</summary>

        <p v-if="store.isLoadingHistory">{{ t('common.loading') }}</p>
        <p v-else-if="historyOpened && store.history.length === 0">
          {{ t('tickets.history.empty') }}
        </p>
        <ul v-else class="history-list">
          <li v-for="entry in store.history" :key="entry.id">{{ historyLine(entry) }}</li>
        </ul>
      </details>

      <AppButton variant="ghost" type="button" @click="onBack">{{ t('tickets.details.backToList') }}</AppButton>
    </div>

    <div
      v-if="knowledgeBaseSearchOpen"
      class="kb-search-overlay"
      @click.self="knowledgeBaseSearchOpen = false"
    >
      <KnowledgeBaseSearchDialog @close="knowledgeBaseSearchOpen = false" />
    </div>
  </div>
</template>

<style scoped>
.ticket-details-view {
  max-width: 40rem;
  margin: var(--space-8) auto;
}

.ticket-detail-body {
  display: flex;
  flex-direction: column;
  gap: var(--space-5);
}

.state-card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-6);
}

.ticket-header {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-5) var(--space-6);
}

.ticket-section {
  padding: var(--space-5) var(--space-6);
}

.sla-badges {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.auto-assigned-badge {
  margin-inline-start: var(--space-2);
  color: var(--text-muted, var(--text-secondary));
  font-size: 0.85em;
}

.history-panel {
  padding: var(--space-5) var(--space-6);
}

.history-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-top: var(--space-3);
}

.ai-assistance {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.ai-assistance__header {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.ai-assistance__header h3 {
  margin: 0;
}

.ai-assistance__actions {
  display: flex;
  gap: var(--space-2);
}

.ai-assistance__summary {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-2);
}
</style>
