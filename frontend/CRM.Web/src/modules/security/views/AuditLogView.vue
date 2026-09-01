<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSecurityStore } from '@/stores/security'
import { useLocale } from '@/composables/useLocale'
import AppButton from '@/components/ui/AppButton.vue'
import AppBadge from '@/components/ui/AppBadge.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import LoadingState from '@/components/ui/LoadingState.vue'
import ErrorState from '@/components/ui/ErrorState.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import type { AuditLogEntry } from '@/types/security'

const { t, te } = useI18n()
const { locale } = useLocale()
const store = useSecurityStore()

function actionLabel(action: string): string | null {
  const key = `security.audit.action.${action}`
  return te(key) ? t(key) : null
}

// Action codes are dot-namespaced ("ticket.created", "security.access.denied");
// the leading segment is a stable category we use only for badge coloring —
// the raw code itself always stays the visible label so filters/searches
// stay copy-pasteable.
const ACTION_CATEGORY_TONE: Record<string, 'neutral' | 'success' | 'warning' | 'danger' | 'info'> = {
  user: 'info',
  security: 'danger',
  ticket: 'success',
  customer: 'success',
}

function actionTone(action: string): 'neutral' | 'success' | 'warning' | 'danger' | 'info' {
  const category = action.split('.')[0] ?? ''
  if (category === 'security' && action.includes('denied')) {
    return 'danger'
  }
  if (action.includes('failed') || action.includes('removed') || action.includes('disabled')) {
    return 'warning'
  }
  return ACTION_CATEGORY_TONE[category] ?? 'neutral'
}

const dateFormatter = computed(
  () => new Intl.DateTimeFormat(locale.value, { dateStyle: 'medium', timeStyle: 'medium' }),
)

function formatDate(value: string): string {
  return dateFormatter.value.format(new Date(value))
}

function actorInitial(entry: AuditLogEntry): string {
  const label = entry.actorEmail ?? t('security.audit.systemActor')
  return label.trim().slice(0, 1).toUpperCase() || '?'
}

const actionFilter = ref('')
const actorIdFilter = ref('')
const targetIdFilter = ref('')
const fromFilter = ref('')
const toFilter = ref('')

const selectedEntry = ref<AuditLogEntry | null>(null)

function currentFilters() {
  return {
    action: actionFilter.value || undefined,
    actorId: actorIdFilter.value || undefined,
    targetId: targetIdFilter.value || undefined,
    from: fromFilter.value ? new Date(fromFilter.value).toISOString() : undefined,
    to: toFilter.value ? new Date(toFilter.value).toISOString() : undefined,
  }
}

function applyFilters() {
  void store.fetchAuditLog({ ...currentFilters(), page: 1 })
}

function onPageChange(page: number) {
  store.setAuditPage(page, currentFilters())
}

function openDetails(entry: AuditLogEntry) {
  selectedEntry.value = entry
}

function closeDetails() {
  selectedEntry.value = null
}

onMounted(() => {
  void store.fetchAuditLog()
})
</script>

<template>
  <div class="audit-log-view">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('navigation.workspace') }}</p>
        <h1>{{ t('security.audit.title') }}</h1>
      </div>
    </div>

    <form class="surface toolbar" @submit.prevent="applyFilters">
      <div class="toolbar-field">
        <label for="audit-action">{{ t('security.audit.filters.action') }}</label>
        <input
          id="audit-action"
          v-model="actionFilter"
          type="text"
          :placeholder="t('security.audit.filters.actionPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-actor">{{ t('security.audit.filters.actor') }}</label>
        <input
          id="audit-actor"
          v-model="actorIdFilter"
          type="text"
          :placeholder="t('security.audit.filters.actorPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-target">{{ t('security.audit.filters.target') }}</label>
        <input
          id="audit-target"
          v-model="targetIdFilter"
          type="text"
          :placeholder="t('security.audit.filters.targetPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-from">{{ t('security.audit.filters.from') }}</label>
        <input
          id="audit-from"
          v-model="fromFilter"
          type="date"
          :placeholder="t('security.audit.filters.fromPlaceholder')"
        />
      </div>
      <div class="toolbar-field">
        <label for="audit-to">{{ t('security.audit.filters.to') }}</label>
        <input
          id="audit-to"
          v-model="toFilter"
          type="date"
          :placeholder="t('security.audit.filters.toPlaceholder')"
        />
      </div>
      <AppButton type="submit">{{ t('security.audit.filters.apply') }}</AppButton>
    </form>

    <LoadingState v-if="store.auditLoading" />
    <ErrorState v-else-if="store.auditError" :retryable="false" :message="t('security.audit.errorLoad')" />
    <EmptyState v-else-if="store.auditEntries.length === 0" :description="t('security.audit.empty')" />

    <div v-else class="surface table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ t('security.audit.columns.occurredAt') }}</th>
            <th>{{ t('security.audit.columns.actor') }}</th>
            <th>{{ t('security.audit.columns.action') }}</th>
            <th>{{ t('security.audit.columns.target') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in store.auditEntries" :key="entry.id" class="audit-row" @click="openDetails(entry)">
            <td class="cell-time" :title="entry.occurredAtUtc">{{ formatDate(entry.occurredAtUtc) }}</td>
            <td>
              <span class="actor-cell">
                <span class="actor-avatar" aria-hidden="true">{{ actorInitial(entry) }}</span>
                {{ entry.actorEmail ?? t('security.audit.systemActor') }}
              </span>
            </td>
            <td>
              <AppBadge :tone="actionTone(entry.action)" class="action-badge">{{ entry.action }}</AppBadge>
              <span v-if="actionLabel(entry.action)" class="action-label">{{ actionLabel(entry.action) }}</span>
            </td>
            <td class="cell-target">
              <span v-if="entry.targetType" class="target-type">{{ entry.targetType }}</span>
              <span v-if="entry.targetId" class="target-id">{{ entry.targetId }}</span>
              <span v-if="!entry.targetType && !entry.targetId" class="target-none">—</span>
            </td>
            <td>
              <AppButton variant="ghost" size="sm" type="button" @click.stop="openDetails(entry)">
                {{ t('security.audit.viewPayload') }}
              </AppButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AppPagination
      v-if="store.auditEntries.length > 0"
      :page="store.auditPage"
      :page-size="store.auditPageSize"
      :total-count="store.auditTotalCount"
      @update:page="onPageChange"
    />

    <div v-if="selectedEntry" class="drawer-overlay" @click.self="closeDetails">
      <aside class="surface drawer">
        <div class="drawer-header">
          <h2>{{ t('security.audit.payloadDrawer.title') }}</h2>
          <AppButton variant="ghost" size="sm" type="button" @click="closeDetails">{{ t('common.close') }}</AppButton>
        </div>

        <div class="drawer-action-row">
          <AppBadge :tone="actionTone(selectedEntry.action)" class="drawer-action-badge">
            {{ selectedEntry.action }}
          </AppBadge>
          <span v-if="actionLabel(selectedEntry.action)" class="action-label">{{ actionLabel(selectedEntry.action) }}</span>
        </div>

        <dl class="drawer-meta">
          <div class="drawer-meta-row">
            <dt>{{ t('security.audit.columns.occurredAt') }}</dt>
            <dd>{{ formatDate(selectedEntry.occurredAtUtc) }}</dd>
          </div>
          <div class="drawer-meta-row">
            <dt>{{ t('security.audit.columns.actor') }}</dt>
            <dd>{{ selectedEntry.actorEmail ?? t('security.audit.systemActor') }}</dd>
          </div>
          <div v-if="selectedEntry.targetType || selectedEntry.targetId" class="drawer-meta-row">
            <dt>{{ t('security.audit.columns.target') }}</dt>
            <dd>{{ selectedEntry.targetType }}: {{ selectedEntry.targetId }}</dd>
          </div>
          <div v-if="selectedEntry.ipAddress" class="drawer-meta-row">
            <dt>{{ t('security.audit.columns.ipAddress') }}</dt>
            <dd>{{ selectedEntry.ipAddress }}</dd>
          </div>
        </dl>

        <h3 class="payload-heading">{{ t('security.audit.payloadDrawer.payloadHeading') }}</h3>
        <pre class="payload-json">{{ selectedEntry.payloadJson ?? t('security.audit.payloadDrawer.empty') }}</pre>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.audit-log-view {
  max-width: 72rem;
  margin: var(--space-8) auto;
}

.drawer-overlay {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  justify-content: flex-end;
  background: rgba(24, 35, 45, 0.4);
}

.drawer {
  width: min(28rem, 90vw);
  height: 100%;
  overflow-y: auto;
  padding: var(--space-6);
}

.drawer-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-4);
}

.audit-row {
  cursor: pointer;
  transition: background-color .15s ease;
}

.audit-row:hover {
  background: var(--canvas);
}

.cell-time {
  white-space: nowrap;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.actor-cell {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
}

.actor-avatar {
  flex: none;
  width: 1.5rem;
  height: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: var(--color-status-info-bg);
  color: var(--color-status-info);
  font-size: var(--font-size-xs);
  font-weight: 700;
}

.action-badge {
  font-family: ui-monospace, 'SFMono-Regular', Consolas, monospace;
}

.action-label {
  display: block;
  margin-top: 0.2rem;
  color: var(--text-muted);
  font-size: var(--font-size-xs);
}

.cell-target {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  font-size: var(--font-size-sm);
}

.target-type {
  color: var(--text-muted);
  text-transform: capitalize;
}

.target-id {
  font-family: ui-monospace, 'SFMono-Regular', Consolas, monospace;
  font-size: var(--font-size-xs);
  color: var(--text-secondary);
}

.target-none {
  color: var(--text-muted);
}

.drawer-action-row {
  margin-bottom: var(--space-4);
}

.drawer-action-badge {
  font-family: ui-monospace, 'SFMono-Regular', Consolas, monospace;
}

.drawer-meta {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  margin: 0 0 var(--space-5);
  padding: var(--space-4);
  background: var(--canvas);
  border-radius: var(--radius-md);
}

.drawer-meta-row {
  display: flex;
  justify-content: space-between;
  gap: var(--space-3);
  font-size: var(--font-size-sm);
}

.drawer-meta-row dt {
  color: var(--text-muted);
}

.drawer-meta-row dd {
  margin: 0;
  color: var(--text-primary);
  font-weight: 600;
  text-align: end;
  word-break: break-word;
}

.payload-heading {
  margin: 0 0 var(--space-2);
  font-size: var(--font-size-md);
}

.payload-json {
  margin: 0;
  padding: var(--space-3);
  background: var(--canvas);
  border-radius: var(--radius-md);
  white-space: pre-wrap;
  word-break: break-word;
  font-size: var(--font-size-sm);
}
</style>
