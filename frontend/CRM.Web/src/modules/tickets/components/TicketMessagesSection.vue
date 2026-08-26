<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTicketMessagesStore } from '@/stores/ticketMessages'
import { useQuickRepliesStore } from '@/stores/quickReplies'
import { fetchEligibleAgents } from '@/api/tickets'
import { useLocale } from '@/composables/useLocale'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppButton from '@/components/ui/AppButton.vue'
import type { EligibleAgent, MessageChannel, Ticket } from '@/types/tickets'

const props = defineProps<{ ticketId: string; ticket?: Ticket | null }>()

const { t } = useI18n()
const { locale } = useLocale()
const store = useTicketMessagesStore()
const quickReplyStore = useQuickRepliesStore()

const isAdding = ref(false)
const draftBody = ref('')
const draftIsInternal = ref(false)
const draftChannel = ref<MessageChannel>('Web')
const draftSubject = ref('')
const textarea = ref<HTMLTextAreaElement | null>(null)

const hasCustomerEmail = computed(() => !!props.ticket?.customerEmail)

const deliveryStatusTone: Record<string, 'neutral' | 'success' | 'warning' | 'danger'> = {
  Pending: 'neutral',
  Sent: 'success',
  Failed: 'danger',
}

watch(
  () => props.ticket?.title,
  (title) => {
    if (draftChannel.value === 'Email' && title) {
      draftSubject.value = title.startsWith('Re: ') ? title : `Re: ${title}`
    }
  },
)

watch(draftChannel, (channel) => {
  if (channel === 'Email' && props.ticket?.title) {
    draftSubject.value = props.ticket.title.startsWith('Re: ')
      ? props.ticket.title
      : `Re: ${props.ticket.title}`
    draftIsInternal.value = false
  }
})

const isQuickReplyOpen = ref(false)
const quickReplySearch = ref('')

const mentionCandidates = ref<EligibleAgent[]>([])
const mentionQuery = ref<string | null>(null)
const mentionedUserIds = ref<string[]>([])
const mentionedUsers = ref<Map<string, string>>(new Map())

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

const remaining = computed(() => 5000 - draftBody.value.length)

const filteredQuickReplies = computed(() => {
  const term = quickReplySearch.value.trim().toLowerCase()
  if (!term) {
    return quickReplyStore.items
  }
  return quickReplyStore.items.filter(
    (q) => q.title.toLowerCase().includes(term) || q.content.toLowerCase().includes(term),
  )
})

const filteredMentionCandidates = computed(() => {
  if (mentionQuery.value === null) {
    return []
  }
  const term = mentionQuery.value.toLowerCase()
  return mentionCandidates.value.filter((a) => a.displayName.toLowerCase().includes(term))
})

onMounted(() => {
  void store.fetchMessages(props.ticketId)
})

function openAddForm() {
  isAdding.value = true
  draftBody.value = ''
  draftIsInternal.value = false
  draftChannel.value = 'Web'
  draftSubject.value = ''
  mentionedUserIds.value = []
  mentionedUsers.value = new Map()
  mentionQuery.value = null
}

function cancelAdd() {
  isAdding.value = false
  draftBody.value = ''
  draftChannel.value = 'Web'
  isQuickReplyOpen.value = false
  mentionQuery.value = null
}

async function submitAdd() {
  const body = draftBody.value.trim()
  if (!body) {
    return
  }
  if (draftChannel.value === 'Email' && !hasCustomerEmail.value) {
    return
  }
  try {
    await store.addMessage(
      props.ticketId,
      body,
      draftIsInternal.value,
      draftIsInternal.value ? mentionedUserIds.value : undefined,
      draftChannel.value,
      draftChannel.value === 'Email' ? draftSubject.value.trim() || undefined : undefined,
    )
    isAdding.value = false
    draftBody.value = ''
    draftChannel.value = 'Web'
    mentionedUserIds.value = []
    mentionedUsers.value = new Map()
  } catch {
    // draft is intentionally preserved; error surfaced via store.error / store.sendError
  }
}

async function toggleQuickReplyPopover() {
  isQuickReplyOpen.value = !isQuickReplyOpen.value
  if (isQuickReplyOpen.value) {
    quickReplySearch.value = ''
    void quickReplyStore.fetch()
  }
}

function insertQuickReply(content: string) {
  draftBody.value = draftBody.value.trim() ? `${draftBody.value}\n${content}` : content
  isQuickReplyOpen.value = false
}

async function onBodyInput(event: Event) {
  const target = event.target as HTMLTextAreaElement
  draftBody.value = target.value

  if (!draftIsInternal.value) {
    mentionQuery.value = null
    return
  }

  const caret = target.selectionStart ?? draftBody.value.length
  const beforeCaret = draftBody.value.slice(0, caret)
  const match = /@(\w*)$/.exec(beforeCaret)

  if (!match) {
    mentionQuery.value = null
    return
  }

  mentionQuery.value = match[1] ?? ''
  if (mentionCandidates.value.length === 0) {
    try {
      mentionCandidates.value = await fetchEligibleAgents()
    } catch {
      mentionCandidates.value = []
    }
  }
}

function selectMention(agent: EligibleAgent) {
  const caret = textarea.value?.selectionStart ?? draftBody.value.length
  const beforeCaret = draftBody.value.slice(0, caret)
  const afterCaret = draftBody.value.slice(caret)
  const replaced = beforeCaret.replace(/@(\w*)$/, `@${agent.displayName} `)

  draftBody.value = replaced + afterCaret
  mentionQuery.value = null

  if (!mentionedUserIds.value.includes(agent.id)) {
    mentionedUserIds.value = [...mentionedUserIds.value, agent.id]
  }
  mentionedUsers.value.set(agent.id, agent.displayName)
}

function removeMention(userId: string) {
  mentionedUserIds.value = mentionedUserIds.value.filter((id) => id !== userId)
}

function highlightMentions(body: string): string[] {
  return body.split(/(@\w+(?:\s\w+)?)/g)
}

function isMentionSegment(segment: string): boolean {
  return segment.startsWith('@')
}
</script>

<template>
  <div class="ticket-messages-section">
    <header class="messages-header">
      <h3>{{ t('tickets.messages.title') }}</h3>
      <AppButton type="button" size="sm" @click="openAddForm" :disabled="isAdding">
        {{ t('tickets.messages.addButton') }}
      </AppButton>
    </header>

    <p v-if="store.error" role="alert">{{ t(`tickets.messages.errors.${store.error}`) }}</p>

    <form v-if="isAdding" class="message-form" @submit.prevent="submitAdd">
      <div class="reply-via">
        <label for="reply-via-select">{{ t('tickets.messages.email.replyVia') }}</label>
        <select id="reply-via-select" v-model="draftChannel">
          <option value="Web">{{ t('tickets.messages.email.channelWeb') }}</option>
          <option value="Email">{{ t('tickets.messages.email.channelEmail') }}</option>
        </select>
      </div>

      <template v-if="draftChannel === 'Email'">
        <p class="email-to-line">
          <strong>{{ t('tickets.messages.email.to') }}:</strong>
          <span v-if="hasCustomerEmail">{{ ticket?.customerName }} &lt;{{ ticket?.customerEmail }}&gt;</span>
          <span v-else class="email-to-missing">{{ t('tickets.messages.email.noCustomerEmail') }}</span>
        </p>
        <AppInput
          v-model="draftSubject"
          :label="t('tickets.messages.email.subject')"
        />
        <AppAlert v-if="store.sendError" tone="danger">{{ t('tickets.messages.email.failed') }}</AppAlert>
      </template>

      <label for="new-message-body">{{ t('tickets.messages.bodyLabel') }}</label>
      <div class="composer-wrap">
        <textarea
          id="new-message-body"
          ref="textarea"
          :value="draftBody"
          maxlength="5000"
          rows="3"
          @input="onBodyInput"
        ></textarea>

        <ul v-if="mentionQuery !== null" class="mention-dropdown">
          <li v-if="filteredMentionCandidates.length === 0" class="mention-empty">
            {{ t('mentions.empty') }}
          </li>
          <li
            v-for="agent in filteredMentionCandidates"
            :key="agent.id"
            class="mention-option"
            @click="selectMention(agent)"
          >
            {{ agent.displayName }}
          </li>
        </ul>
      </div>

      <p v-if="remaining <= 200" class="char-warning">
        {{ t('tickets.messages.charsRemaining', { count: remaining }) }}
      </p>

      <ul v-if="draftIsInternal && mentionedUserIds.length > 0" class="mention-chips">
        <li v-for="userId in mentionedUserIds" :key="userId" class="mention-chip">
          {{ mentionedUsers.get(userId) ?? userId }}
          <button type="button" @click="removeMention(userId)" :aria-label="t('common.close')">×</button>
        </li>
      </ul>

      <label v-if="draftChannel === 'Web'" class="internal-toggle">
        <input type="checkbox" v-model="draftIsInternal" />
        {{ t('tickets.messages.internalToggle') }}
      </label>

      <div class="message-form-actions">
        <div class="quick-reply-control">
          <AppButton type="button" variant="secondary" size="sm" @click="toggleQuickReplyPopover">
            {{ t('quickReplies.button') }}
          </AppButton>
          <div v-if="isQuickReplyOpen" class="quick-reply-popover">
            <input
              type="text"
              v-model="quickReplySearch"
              :placeholder="t('quickReplies.search')"
            />
            <p v-if="quickReplyStore.loading">{{ t('quickReplies.loading') }}</p>
            <p v-else-if="filteredQuickReplies.length === 0">{{ t('quickReplies.empty') }}</p>
            <ul v-else class="quick-reply-list">
              <li
                v-for="reply in filteredQuickReplies"
                :key="reply.id"
                @click="insertQuickReply(reply.content)"
              >
                <strong>{{ reply.title }}</strong>
                <span>{{ reply.content }}</span>
              </li>
            </ul>
          </div>
        </div>

        <AppButton
          type="submit"
          size="sm"
          :disabled="store.saving || !draftBody.trim() || (draftChannel === 'Email' && !hasCustomerEmail)"
        >
          {{
            store.saving
              ? t('tickets.messages.email.sending')
              : draftChannel === 'Email'
                ? t('tickets.messages.email.send')
                : t('tickets.messages.save')
          }}
        </AppButton>
        <AppButton type="button" variant="secondary" size="sm" @click="cancelAdd">
          {{ t('tickets.messages.cancel') }}
        </AppButton>
      </div>
    </form>

    <ul v-if="store.loading" class="skeleton">
      <li></li>
      <li></li>
      <li></li>
    </ul>

    <div v-else-if="store.items.length === 0 && !isAdding">
      <p>{{ t('tickets.messages.empty') }}</p>
    </div>

    <ul v-else class="messages-list">
      <li
        v-for="message in store.items"
        :key="message.id"
        class="message-item"
        :class="{ internal: message.isInternal }"
      >
        <p class="message-meta">
          <span>{{ message.authorDisplayName }}</span>
          <span>{{ formatDate(message.createdAtUtc) }}</span>
          <span v-if="message.isInternal" class="internal-badge">
            {{ t('tickets.messages.internalBadge') }}
          </span>
          <span v-else class="public-badge">{{ t('tickets.messages.publicBadge') }}</span>
          <AppBadge
            v-if="message.channel === 'Email' && message.emailDeliveryStatus"
            :tone="deliveryStatusTone[message.emailDeliveryStatus] ?? 'neutral'"
          >
            {{ t(`tickets.messages.email.${message.emailDeliveryStatus.toLowerCase()}`) }}
          </AppBadge>
        </p>
        <p class="message-body">
          <template v-for="(segment, index) in highlightMentions(message.body)" :key="index">
            <strong v-if="message.mentionedUserIds.length > 0 && isMentionSegment(segment)" class="mention-highlight">{{ segment }}</strong>
            <template v-else>{{ segment }}</template>
          </template>
        </p>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.messages-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.messages-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.message-item {
  border-bottom: 1px solid var(--line);
  padding-bottom: var(--space-3);
  border-inline-start: 3px solid transparent;
  padding-inline-start: var(--space-2);
}

.message-item.internal {
  border-inline-start-color: var(--color-status-warning);
  background: var(--color-status-warning-bg);
}

.message-meta {
  display: flex;
  gap: var(--space-4);
  color: var(--muted);
  font-size: var(--font-size-xs);
  font-weight: 700;
}

.internal-badge {
  color: var(--color-status-warning);
}

.public-badge {
  color: var(--color-status-success);
}

.mention-highlight {
  color: var(--teal-dark);
}

.message-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin: var(--space-2) 0;
}

.composer-wrap {
  position: relative;
}

.mention-dropdown {
  position: absolute;
  z-index: var(--z-drawer);
  inset-inline-start: 0;
  top: 100%;
  width: 100%;
  max-height: 10rem;
  overflow-y: auto;
  margin: 0;
  padding: 0;
  list-style: none;
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  box-shadow: var(--shadow-md);
}

.mention-option {
  padding: var(--space-2) var(--space-3);
  cursor: pointer;
}

.mention-option:hover {
  background: #f5fbf9;
}

.mention-empty {
  padding: var(--space-2) var(--space-3);
  color: var(--muted);
}

.mention-chips {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  list-style: none;
  padding: 0;
  margin: 0;
}

.mention-chip {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding: 0.2rem var(--space-2);
  background: #edf2f2;
  border-radius: 999px;
}

.mention-chip button {
  padding: 0;
  color: inherit;
  background: transparent;
}

.internal-toggle {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.reply-via {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.reply-via select {
  width: auto;
}

.email-to-line {
  margin: 0;
  color: var(--muted);
  font-size: var(--font-size-sm);
}

.email-to-missing {
  color: var(--color-status-danger);
}

.char-warning {
  color: var(--color-status-danger);
}

.message-form-actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.quick-reply-control {
  position: relative;
}

.quick-reply-popover {
  position: absolute;
  z-index: var(--z-drawer);
  inset-inline-start: 0;
  bottom: 100%;
  width: 20rem;
  padding: var(--space-4);
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-md);
}

.quick-reply-list {
  list-style: none;
  padding: 0;
  margin: var(--space-2) 0 0;
  max-height: 14rem;
  overflow-y: auto;
}

.quick-reply-list li {
  display: flex;
  flex-direction: column;
  padding: var(--space-2);
  cursor: pointer;
  border-radius: var(--radius-sm);
}

.quick-reply-list li:hover {
  background: #f5fbf9;
}

.quick-reply-list span {
  overflow: hidden;
  color: var(--muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 640px) {
  .quick-reply-popover {
    position: fixed;
    inset: auto 0 0 0;
    width: 100%;
    max-height: 60vh;
    border-radius: var(--radius-lg) var(--radius-lg) 0 0;
  }
}

.skeleton {
  list-style: none;
  padding: 0;
}

.skeleton li {
  height: 2rem;
  margin-bottom: var(--space-2);
  background: var(--canvas);
  border-radius: var(--radius-sm);
}
</style>
