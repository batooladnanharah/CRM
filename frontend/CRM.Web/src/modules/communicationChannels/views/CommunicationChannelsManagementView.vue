<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLocale } from '@/composables/useLocale'
import { useCommunicationChannelsStore } from '@/stores/communicationChannels'
import AppButton from '@/components/ui/AppButton.vue'
import AppAlert from '@/components/ui/AppAlert.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { Channel } from '@/types/communicationChannels'

const { t } = useI18n()
const { locale } = useLocale()
const store = useCommunicationChannelsStore()

const isAdding = ref(false)
const draftName = ref('')

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'short' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

const selectedChannel = computed(
  () => store.channels.find((c) => c.id === store.selectedChannelId) ?? null,
)

onMounted(() => {
  void store.fetchChannels()
})

function openAddForm() {
  isAdding.value = true
  draftName.value = ''
}

function cancelAdd() {
  isAdding.value = false
  draftName.value = ''
}

async function submitAdd() {
  const name = draftName.value.trim()
  if (!name) {
    return
  }
  try {
    await store.create({ name, type: 'Email' })
    isAdding.value = false
    draftName.value = ''
  } catch {
    // error surfaced via store.error
  }
}

async function onToggleEnabled(channel: Channel) {
  try {
    await store.update(channel.id, { name: channel.name, isEnabled: !channel.isEnabled })
  } catch {
    // error surfaced via store.error
  }
}

async function onDelete(channel: Channel) {
  if (!window.confirm(t('communicationChannels.deleteConfirm'))) {
    return
  }
  try {
    await store.remove(channel.id)
  } catch {
    // error surfaced via store.error
  }
}

async function onSelect(channel: Channel) {
  await store.selectChannel(channel.id)
}
</script>

<template>
  <div class="communication-channels-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('communicationChannels.title') }}</h1>
      </div>
      <AppButton type="button" @click="openAddForm" :disabled="isAdding">
        {{ t('communicationChannels.new') }}
      </AppButton>
    </div>

    <AppAlert v-if="store.error" tone="danger" role="alert">{{ t(`communicationChannels.errors.${store.error}`) }}</AppAlert>

    <div class="channels-layout">
      <section class="surface channels-pane">
        <h3>{{ t('communicationChannels.channelsHeading') }}</h3>

        <form v-if="isAdding" class="channel-form" @submit.prevent="submitAdd">
          <label for="channel-name">{{ t('communicationChannels.fields.name') }}</label>
          <input id="channel-name" v-model="draftName" type="text" maxlength="200" />
          <p class="channel-type-hint">{{ t('communicationChannels.fields.typeEmailOnly') }}</p>
          <div class="form-actions">
            <AppButton type="submit" :loading="store.saving" :disabled="!draftName.trim()">
              {{ store.saving ? t('quickReplies.saving') : t('common.save') }}
            </AppButton>
            <AppButton variant="secondary" type="button" @click="cancelAdd">{{ t('common.cancel') }}</AppButton>
          </div>
        </form>

        <LoadingState v-if="store.loading" />
        <EmptyState v-else-if="store.channels.length === 0 && !isAdding" :description="t('communicationChannels.empty')" />

        <ul v-else class="channels-list">
          <li
            v-for="channel in store.channels"
            :key="channel.id"
            class="channel-item"
            :class="{ selected: channel.id === store.selectedChannelId }"
          >
            <button type="button" class="channel-select" @click="onSelect(channel)">
              <strong>{{ channel.name }}</strong>
              <span>{{ channel.type }}</span>
              <AppBadge v-if="!channel.isEnabled" tone="neutral">
                {{ t('communicationChannels.disabledBadge') }}
              </AppBadge>
            </button>
            <div class="channel-actions">
              <AppButton variant="ghost" size="sm" type="button" @click="onToggleEnabled(channel)">
                {{ channel.isEnabled ? t('communicationChannels.disable') : t('communicationChannels.enable') }}
              </AppButton>
              <AppButton variant="ghost" size="sm" type="button" @click="onDelete(channel)">{{ t('quickReplies.delete') }}</AppButton>
            </div>
          </li>
        </ul>
      </section>

      <section class="surface emails-pane">
        <h3>{{ t('communicationChannels.emailsHeading') }}</h3>

        <EmptyState v-if="!selectedChannel" :description="t('communicationChannels.selectChannelPrompt')" />

        <template v-else>
          <p class="selected-channel-name">{{ selectedChannel.name }}</p>

          <LoadingState v-if="store.emailsLoading" />
          <EmptyState v-else-if="store.emails.length === 0" :description="t('communicationChannels.emptyEmails')" />

          <div v-else class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{{ t('communicationChannels.fields.fromAddress') }}</th>
                  <th>{{ t('communicationChannels.fields.subject') }}</th>
                  <th>{{ t('communicationChannels.fields.receivedAt') }}</th>
                  <th>{{ t('communicationChannels.fields.ticketId') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="email in store.emails" :key="email.id">
                  <td class="truncate" :title="email.fromAddress">{{ email.fromAddress }}</td>
                  <td class="truncate" :title="email.subject">{{ email.subject }}</td>
                  <td>{{ formatDate(email.receivedAtUtc) }}</td>
                  <td>{{ email.ticketId ?? '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </template>
      </section>
    </div>
  </div>
</template>

<style scoped>
.communication-channels-view {
  max-width: 70rem;
  margin: var(--space-8) auto;
}

.channels-layout {
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: var(--space-5);
}

.channels-pane,
.emails-pane {
  padding: var(--space-5);
}

.channel-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-bottom: var(--space-4);
}

.channel-type-hint {
  color: var(--muted);
}

.form-actions {
  display: flex;
  gap: var(--space-2);
}

.channels-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.channel-item {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  padding: var(--space-2);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
}

.channel-item.selected {
  border-color: var(--teal);
}

.channel-select {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-1);
  padding: 0;
  color: inherit;
  background: transparent;
}

.channel-actions {
  display: flex;
  gap: var(--space-2);
}

.selected-channel-name {
  font-weight: 700;
}

.truncate {
  max-width: 16rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 900px) {
  .channels-layout {
    grid-template-columns: 1fr;
  }
}
</style>
